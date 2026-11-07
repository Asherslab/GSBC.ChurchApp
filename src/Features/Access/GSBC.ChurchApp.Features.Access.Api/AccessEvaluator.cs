using System.Security.Claims;
using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessEvaluator(
    AccessDbContext db, GrantCache cache, PermissionCatalog catalog, IScopeHierarchy hierarchy, ToolCallers tools, TimeProvider time)
    : IAccessEvaluator
{
    public async ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken)
    {
        if (Caller.UserId(user) is not { } userId || !catalog.ByNode.TryGetValue(node, out Permission? permission))
            return false;

        GrantSet grants = AsSeenBy(await CompiledAsync(userId, cancellationToken), Caller.IsTool(user), catalog);
        return grants.IsAllowed(permission, string.IsNullOrEmpty(scopeId) ? null : scopeId);
    }

    public async ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) =>
        AsSeenBy(await CompiledAsync(userId, cancellationToken), tools.ActsThroughATool(userId), catalog);

    public static GrantSet AsSeenBy(GrantSet grants, bool throughATool, PermissionCatalog catalog) =>
        throughATool ? grants.Refusing(catalog.Sensitive) : grants;

    private async ValueTask<GrantSet> CompiledAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (cache.TryGet(userId, out GrantSet cached))
            return cached;

        List<Assignment> assignments = await db.ActiveAssignments(time.GetUtcNow().UtcDateTime)
            .Where(assignment => assignment.UserId == userId)
            .ToListAsync(cancellationToken);
        Dictionary<Guid, RoleRecord> roles = await db.LoadRolesWithIncludesAsync(assignments.Select(assignment => assignment.RoleId), cancellationToken);

        GrantSet compiled = await RoleGrants.CompileAsync(
            assignments.Select(assignment => (assignment.RoleId, assignment.ScopeObjectId)), roles, hierarchy, cancellationToken);
        cache.Set(userId, compiled);
        return compiled;
    }
}
