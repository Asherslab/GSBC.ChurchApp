using GSBC.ChurchApp.Platform.Data.Email;

namespace GSBC.ChurchApp.Platform.Api.Email;

internal sealed class QueuedEmailSender(EmailDbContext db, EmailSignal signal, TimeProvider time) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        db.OutgoingEmails.Add(new OutgoingEmail
        {
            To = message.To,
            Subject = message.Subject,
            Body = message.Body,
            CreatedAt = now,
            NextAttemptAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        signal.Wake();
    }
}

public sealed class EmailSignal
{
    private readonly SemaphoreSlim _wake = new(0);

    public void Wake()
    {
        if (_wake.CurrentCount == 0)
            _wake.Release();
    }

    public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) => _wake.WaitAsync(timeout, cancellationToken);
}
