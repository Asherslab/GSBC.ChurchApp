using System.Security.Claims;
using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessEvaluator(AccessDbContext db, GrantCache cache, PermissionCatalog catalog, TimeProvider time) : IAccessEvaluator
{
    public async ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken)
    {
        if (Caller.UserId(user) is not { } userId || !catalog.ByNode.TryGetValue(node, out Permission? permission))
            return false;

        GrantSet grants = await GrantsForAsync(userId, cancellationToken);
        return grants.IsAllowed(permission, string.IsNullOrEmpty(scopeId) ? null : scopeId);
    }

    public async ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (cache.TryGet(userId, out GrantSet cached))
            return cached;

        DateTime now = time.GetUtcNow().UtcDateTime;
        var assignments = await db.Assignments.AsNoTracking()
            .Where(assignment => assignment.UserId == userId && (assignment.ExpiresAt == null || assignment.ExpiresAt > now))
            .Select(assignment => new { assignment.RoleId, assignment.ScopeObjectId })
            .ToListAsync(cancellationToken);

        Dictionary<Guid, RoleRecord> roles = await db.LoadRolesWithIncludesAsync(assignments.Select(assignment => assignment.RoleId), cancellationToken);
        List<Grant> grants = [];
        foreach (var assignment in assignments)
        {
            if (!roles.TryGetValue(assignment.RoleId, out RoleRecord? role) || !ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind))
                continue;
            grants.AddRange(RoleGrants.At(PatternsOf(role, roles), kind, assignment.ScopeObjectId));
        }

        GrantSet compiled = new(grants);
        cache.Set(userId, compiled);
        return compiled;
    }

    public static IEnumerable<string> PatternsOf(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles) =>
        role.Patterns.Select(pattern => pattern.Pattern)
            .Concat(role.Includes
                .Select(include => roles.GetValueOrDefault(include.IncludedRoleId))
                .OfType<RoleRecord>()
                .SelectMany(included => included.Patterns.Select(pattern => pattern.Pattern)));
}
