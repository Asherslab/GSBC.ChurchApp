using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Platform.Api.Email;

internal sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email to {To}: {Subject}\n{Body}", message.To, message.Subject, message.Body);
        return Task.CompletedTask;
    }
}

internal sealed class UnconfiguredEmailSender(ILogger<UnconfiguredEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogWarning("Email isn't configured; dropped \"{Subject}\" to one recipient", message.Subject);
        return Task.CompletedTask;
    }
}
