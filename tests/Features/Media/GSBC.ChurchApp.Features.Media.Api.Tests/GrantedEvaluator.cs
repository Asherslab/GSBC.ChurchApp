using System.Security.Claims;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

internal sealed class GrantedEvaluator : IAccessEvaluator
{
    private readonly HashSet<(string User, string Node, string? Scope)> _grants = [];

    public void Grant(ClaimsPrincipal user, string node, string? scope) => _grants.Add((IdOf(user), node, scope));

    public void Revoke(ClaimsPrincipal user, string node, string? scope) => _grants.Remove((IdOf(user), node, scope));

    public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_grants.Contains((IdOf(user), node, scopeId)));

    public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);

    private static string IdOf(ClaimsPrincipal user) => user.FindFirstValue(Caller.UserIdClaim)!;
}
