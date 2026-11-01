using GSBC.ChurchApp.Features.Accounts.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class AccountsCleanup(IServiceScopeFactory scopes, TimeProvider time, ILogger<AccountsCleanup> logger) : BackgroundService
{
    public static readonly TimeSpan AbandonedAfter = TimeSpan.FromDays(7);
    public static readonly TimeSpan CodesKeptFor = TimeSpan.FromDays(30);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private const long AdvisoryLockKey = 0x6773_6263_636c_6e75;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval, time);
        do
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                await CleanAsync(scope.ServiceProvider.GetRequiredService<AccountsDbContext>(), time.GetUtcNow().UtcDateTime, stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Cleaning up accounts failed; trying again next time");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public static async Task CleanAsync(AccountsDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({AdvisoryLockKey})", cancellationToken);

        DateTime abandoned = now - AbandonedAfter;
        await db.Users
            .Where(user => user.CreatedAt < abandoned
                && !db.Set<IdentityUserPasskey<Guid>>().Any(passkey => passkey.UserId == user.Id)
                && !db.Codes.Any(code => code.UserId == user.Id && code.UsedAt == null && code.ExpiresAt > now
                    && (code.Purpose == CodePurpose.Invitation || code.Purpose == CodePurpose.RegisterPasskey)))
            .ExecuteDeleteAsync(cancellationToken);

        await db.Codes.Where(code => code.ExpiresAt < now - CodesKeptFor).ExecuteDeleteAsync(cancellationToken);
        await db.Sessions.Where(session => session.ExpiresAt < now).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
