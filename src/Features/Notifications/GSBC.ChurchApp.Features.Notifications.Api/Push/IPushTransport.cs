namespace GSBC.ChurchApp.Features.Notifications.Api.Push;

public sealed record PushTarget(string Platform, string AppId, string Environment, string Token);

public sealed record PushMessage(
    string? Title,
    string? Body,
    string? Link,
    string? NotificationId,
    string? ThreadId,
    string? CollapseId,
    int? Badge,
    bool Urgent,
    bool Background);

public enum PushOutcome
{
    Sent,
    Retry,
    DeadToken,
    Failed,
    Off,
}

public sealed record PushResult(PushOutcome Outcome, string? Error = null)
{
    public static readonly PushResult Sent = new(PushOutcome.Sent);
}

public interface IPushTransport
{
    Task<PushResult> SendAsync(PushTarget target, PushMessage message, CancellationToken cancellationToken);
}

internal sealed class PushOff : IPushTransport
{
    public Task<PushResult> SendAsync(PushTarget target, PushMessage message, CancellationToken cancellationToken) =>
        Task.FromResult(new PushResult(PushOutcome.Off, "Push is off: no APNs key configured."));
}
