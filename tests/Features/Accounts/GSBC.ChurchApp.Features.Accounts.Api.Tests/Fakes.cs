using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

internal sealed class NoRoles : IRoleAssignments
{
    public ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<RoleHolder>>([]);

    public ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<AssignmentOutcome> GiveAsync(Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask ScopesChangedAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask MoveScopeAsync(ScopeKind kind, string fromObjectId, string toObjectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<int> HoldingCountAsync(string roleKey, string objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask ForgetScopeAsync(ScopeKind kind, IReadOnlyCollection<string> objectIds, CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class NoGrants : IAccessEvaluator
{
    public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);

    public ValueTask<bool> IsAllowedAsync(System.Security.Claims.ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
