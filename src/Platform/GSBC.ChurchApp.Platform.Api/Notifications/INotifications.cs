using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Notifications;

public sealed record Notification(
    NotificationKind Kind,
    NotificationAudience Audience,
    string Title,
    string Body,
    string? Link = null,
    string? Subject = null,
    bool Urgent = false,
    string? ScopeId = null,
    bool Test = false,
    string? Action = null)
{
    public required Guid? Actor { get; init; }
}

public sealed record NotificationDigestItem(
    NotificationKind Kind,
    NotificationAudience Audience,
    string Group,
    string Link,
    DateTime? StartedAt = null,
    string? ScopeId = null,
    string? Action = null)
{
    public required Guid? Actor { get; init; }
}

public abstract record NotificationAudience
{
    public static readonly NotificationAudience Everyone = new EveryoneAudience();
    public static readonly NotificationAudience SignedIn = new SignedInAudience();

    public static NotificationAudience Person(Guid userId) => new PersonAudience(userId);

    public static NotificationAudience Holding(Permission permission, string? objectId = null) => new HoldingAudience(permission, objectId);

    public sealed record EveryoneAudience : NotificationAudience;

    public sealed record SignedInAudience : NotificationAudience;

    public sealed record PersonAudience(Guid UserId) : NotificationAudience;

    public sealed record HoldingAudience(Permission Permission, string? ObjectId) : NotificationAudience;
}

public interface INotifications
{
    Task RaiseAsync(Notification notification, CancellationToken cancellationToken);

    Task RaiseOneOfAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken);

    Task SettleAsync(string subject, CancellationToken cancellationToken);

    Task RecallAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;

    Task<NotificationReach> ReachAsync(NotificationAudience audience, CancellationToken cancellationToken) => Task.FromResult(new NotificationReach(0, 0));

    Task CollectAsync(NotificationDigestItem item, CancellationToken cancellationToken);

    Task ForgetDigestAsync(NotificationKind kind, string group, CancellationToken cancellationToken);
}

internal sealed class NoNotifications : INotifications
{
    public Task RaiseAsync(Notification notification, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RaiseOneOfAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SettleAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task CollectAsync(NotificationDigestItem item, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ForgetDigestAsync(NotificationKind kind, string group, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed record NotificationReach(int People, int Phones);

public sealed record NotificationKindSet(IReadOnlyList<NotificationKind> Kinds);
