using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Access;

public interface IRoleAssignments
{
    ValueTask<AssignmentOutcome> GiveAsync(Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken);

    ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken);

    ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken);

    ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken);

    ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken);

    async ValueTask<IReadOnlyList<RoleHolding>> HoldingsAsync(string roleKey, IReadOnlyCollection<string> objectIds, CancellationToken cancellationToken)
    {
        List<RoleHolding> holdings = [];
        foreach (string objectId in objectIds)
            holdings.AddRange((await HoldersAsync(roleKey, objectId, cancellationToken)).Select(holder => new RoleHolding(holder.UserId, objectId)));
        return holdings;
    }

    ValueTask ScopesChangedAsync(CancellationToken cancellationToken);

    ValueTask MoveScopeAsync(ScopeKind kind, string fromObjectId, string toObjectId, CancellationToken cancellationToken);

    ValueTask<int> HoldingCountAsync(string roleKey, string objectId, CancellationToken cancellationToken);

    ValueTask ForgetScopeAsync(ScopeKind kind, IReadOnlyCollection<string> objectIds, CancellationToken cancellationToken);
}

public sealed record AssignmentOutcome(bool Done, string? Refusal)
{
    public static readonly AssignmentOutcome Success = new(true, null);

    public static AssignmentOutcome Refused(string reason) => new(false, reason);
}

public sealed record RoleHolder(Guid UserId);

public sealed record RoleHolding(Guid UserId, string ObjectId);
