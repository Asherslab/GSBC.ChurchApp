using GSBC.ChurchApp.Platform.Data.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Platform.Api.Email;

public sealed class EmailDispatcher(EmailDbContext db, IEmailTransport transport, ILogger<EmailDispatcher> logger)
{
    public const int MaximumAttempts = 40;
    private const int BatchSize = 10;
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan LongestBackoff = TimeSpan.FromHours(6);

    public async Task<int> DeliverDueAsync(DateTime now, CancellationToken cancellationToken)
    {
        DateTime leaseUntil = now + Lease;
        List<OutgoingEmail> due = await db.OutgoingEmails.FromSql($"""
            UPDATE email.outgoing_emails SET lease_until = {leaseUntil}
            WHERE id IN (
                SELECT id FROM email.outgoing_emails
                WHERE sent_at IS NULL AND gave_up_at IS NULL AND next_attempt_at <= {now}
                  AND (lease_until IS NULL OR lease_until < {now})
                ORDER BY id LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED)
            RETURNING *
            """).ToListAsync(cancellationToken);

        foreach (OutgoingEmail email in due)
        {
            if (ReservedAddresses.IsReserved(email.To))
            {
                email.GaveUpAt = now;
                email.LastError = ReservedAddresses.NotSent;
                email.LeaseUntil = null;
                logger.LogInformation("Didn't send email {EmailId}: its address is a reserved test address", email.Id);
                continue;
            }

            try
            {
                await transport.SendAsync(new EmailMessage(email.To, email.Subject, email.Body), cancellationToken);
                email.SentAt = now;
                email.LastError = null;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                email.Attempts++;
                email.LastError = exception.Message;
                email.NextAttemptAt = now + Backoff(email.Attempts);
                if (email.Attempts >= MaximumAttempts)
                {
                    email.GaveUpAt = now;
                    logger.LogCritical("Gave up on email {EmailId} after {Attempts} attempts: {Error}", email.Id, email.Attempts, exception.Message);
                }
            }
            email.LeaseUntil = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return due.Count;
    }

    public static TimeSpan Backoff(int attempts) =>
        attempts <= 9 ? TimeSpan.FromMinutes(Math.Pow(2, attempts - 1)) : LongestBackoff;
}

internal sealed class EmailDelivery(IServiceScopeFactory scopes, EmailSignal signal, TimeProvider time, ILogger<EmailDelivery> logger)
    : BackgroundService
{
    private static readonly TimeSpan IdleCheck = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                EmailDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<EmailDispatcher>();
                while (await dispatcher.DeliverDueAsync(time.GetUtcNow().UtcDateTime, stoppingToken) > 0)
                {
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Delivering email failed; trying again shortly");
            }

            await signal.WaitAsync(IdleCheck, stoppingToken);
        }
    }
}
