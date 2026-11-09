using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

internal sealed class QuietNotifications : INotifications
{
    public Task RaiseAsync(Notification notification, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RaiseOneOfAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SettleAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class RecordedRoles : IRoleAssignments, IPhoneRoles
{
    public HashSet<(Guid Holder, string Role, string? ObjectId, bool Phone)> Held { get; } = [];

    public ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken)
    {
        Held.Add((userId, roleKey, objectId, false));
        return ValueTask.CompletedTask;
    }

    public ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken)
    {
        Held.Remove((userId, roleKey, objectId, false));
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<RoleHolder>>(Held.Where(held => !held.Phone && held.Role == roleKey && held.ObjectId == objectId).Select(held => new RoleHolder(held.Holder)).ToList());

    public ValueTask GiveAsync(Guid installId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken)
    {
        Held.Add((installId, roleKey, objectId, true));
        return ValueTask.CompletedTask;
    }

    public ValueTask TakeAwayAsync(Guid installId, string roleKey, string? objectId, CancellationToken cancellationToken)
    {
        Held.Remove((installId, roleKey, objectId, true));
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<HeldRole>> HeldAsync(Guid installId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<HeldRole>>(Held.Where(held => held.Phone && held.Holder == installId).Select(held => new HeldRole(held.Role, held.ObjectId, null)).ToList());

    public ValueTask MoveToPersonAsync(Guid installId, Guid userId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask<AssignmentOutcome> GiveAsync(Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask ScopesChangedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask MoveScopeAsync(ScopeKind kind, string fromObjectId, string toObjectId, CancellationToken cancellationToken)
    {
        foreach (var held in Held.Where(held => held.ObjectId == fromObjectId).ToList())
        {
            Held.Remove(held);
            Held.Add(held with { ObjectId = toObjectId });
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask<int> HoldingCountAsync(string roleKey, string objectId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Held.Count(held => held.Role == roleKey && held.ObjectId == objectId));

    public ValueTask ForgetScopeAsync(ScopeKind kind, IReadOnlyCollection<string> objectIds, CancellationToken cancellationToken)
    {
        Held.RemoveWhere(held => held.ObjectId is { } objectId && objectIds.Contains(objectId));
        return ValueTask.CompletedTask;
    }
}
