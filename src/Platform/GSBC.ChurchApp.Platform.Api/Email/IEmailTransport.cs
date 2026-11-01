using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Platform.Api.Email;

public interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

internal sealed class DevelopmentEmailTransport(ILogger<DevelopmentEmailTransport> logger) : IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email to {To}: {Subject}\n{Body}", message.To, message.Subject, message.Body);
        return Task.CompletedTask;
    }
}

internal sealed class UnconfiguredEmailTransport : IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("No email transport is configured; the message stays queued.");
}
