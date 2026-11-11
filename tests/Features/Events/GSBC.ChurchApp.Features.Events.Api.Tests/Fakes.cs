using System.Security.Claims;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

internal sealed class Grants : IAccessEvaluator
{
    private readonly HashSet<(Guid, string, string?)> _allowed = [];

    public void Allow(Guid person, string node, string? calendarId) => _allowed.Add((person, node, calendarId));

    public void Refuse(Guid person, string node, string? calendarId) => _allowed.Remove((person, node, calendarId));

    public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Caller.UserId(user) is { } id && (_allowed.Contains((id, node, null)) || (scopeId is not null && _allowed.Contains((id, node, scopeId)))));

    public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new GrantSet(_allowed.Where(grant => grant.Item1 == userId && grant.Item3 is null)
            .Select(grant => new Grant(PermissionPattern.Parse(grant.Item2), Scope.Global)).ToList()));
}

internal sealed class RecordedNotifications : INotifications
{
    public List<Notification> Raised { get; } = [];

    public Task RaiseAsync(Notification notification, CancellationToken cancellationToken) => RaiseOneOfAsync([notification], cancellationToken);

    public Task RaiseOneOfAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken)
    {
        foreach (Notification notification in mostSpecificFirst)
            if (notification.Subject is not null && LinkTargets.InAppRoute(notification.Link ?? "") is null)
                throw new ArgumentException($"A notification about '{notification.Subject}' must link to it in the app.", nameof(mostSpecificFirst));
        Raised.AddRange(mostSpecificFirst.Select(NotificationLinks.Checked));
        return Task.CompletedTask;
    }

    public Task SettleAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task CollectAsync(NotificationDigestItem item, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ForgetDigestAsync(NotificationKind kind, string group, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class RecordedRoles : IRoleAssignments, IPhoneRoles
{
    private HashSet<(Guid Holder, string Role, string? ObjectId, bool Phone)> Held { get; } = [];

    public ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
        Add((userId, roleKey, objectId, false));

    public ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) =>
        Remove((userId, roleKey, objectId, false));

    public ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<RoleHolder>>(Held.Where(held => !held.Phone && held.Role == roleKey && held.ObjectId == objectId).Select(held => new RoleHolder(held.Holder)).ToList());

    public ValueTask GiveAsync(Guid installId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
        Add((installId, roleKey, objectId, true));

    public ValueTask TakeAwayAsync(Guid installId, string roleKey, string? objectId, CancellationToken cancellationToken) =>
        Remove((installId, roleKey, objectId, true));

    public ValueTask<IReadOnlyList<HeldRole>> HeldAsync(Guid installId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<HeldRole>>(Held.Where(held => held.Phone && held.Holder == installId).Select(held => new HeldRole(held.Role, held.ObjectId, null)).ToList());

    public ValueTask MoveToPersonAsync(Guid installId, Guid userId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask<AssignmentOutcome> GiveAsync(Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask ScopesChangedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask MoveScopeAsync(ScopeKind kind, string fromObjectId, string toObjectId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask<int> HoldingCountAsync(string roleKey, string objectId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Held.Count(held => held.Role == roleKey && held.ObjectId == objectId));

    public ValueTask ForgetScopeAsync(ScopeKind kind, IReadOnlyCollection<string> objectIds, CancellationToken cancellationToken)
    {
        Held.RemoveWhere(held => held.ObjectId is { } objectId && objectIds.Contains(objectId));
        return ValueTask.CompletedTask;
    }

    private ValueTask Add((Guid, string, string?, bool) held)
    {
        Held.Add(held);
        return ValueTask.CompletedTask;
    }

    private ValueTask Remove((Guid, string, string?, bool) held)
    {
        Held.Remove(held);
        return ValueTask.CompletedTask;
    }
}

internal sealed class AnyMedia : IMediaReferences
{
    public Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaRef>>(claimed.ToList());

    public Task UseAsync(string owner, IReadOnlyCollection<MediaRef> verified, CancellationToken cancellationToken) => Task.CompletedTask;
}
