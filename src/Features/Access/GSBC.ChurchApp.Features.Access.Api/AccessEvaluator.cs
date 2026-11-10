using System.Security.Claims;
using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessEvaluator(
    AccessDbContext db, GrantCache cache, PermissionCatalog catalog, IScopeHierarchy hierarchy, IEnumerable<IScopeAttachments> attachments,
    HeldThroughRules throughRules, ToolCallers tools, TimeProvider time)
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

        DateTime now = time.GetUtcNow().UtcDateTime;
        List<(Guid RoleId, string? ObjectId)> assignments = (await db.ActiveAssignments(now)
                .Where(assignment => assignment.UserId == userId)
                .ToListAsync(cancellationToken))
            .Select(assignment => (assignment.RoleId, assignment.ScopeObjectId))
            .ToList();
        assignments.AddRange(await HeldThroughAsync(assignments, now, cancellationToken));
        Dictionary<Guid, RoleRecord> roles = await db.LoadRolesWithIncludesAsync(assignments.Select(assignment => assignment.RoleId), cancellationToken);

        GrantSet compiled = await RoleGrants.CompileAsync(assignments, roles, hierarchy, cancellationToken, attachments);
        cache.Set(userId, compiled);
        return compiled;
    }

    private async Task<List<(Guid RoleId, string? ObjectId)>> HeldThroughAsync(
        List<(Guid RoleId, string? ObjectId)> assignments, DateTime now, CancellationToken cancellationToken)
    {
        if (!throughRules.Any)
            return [];
        IReadOnlyList<HeldThroughRule> rules = await throughRules.ResolveAsync(db, cancellationToken);
        HashSet<Guid> personRoles = rules.Select(rule => rule.PersonRoleId).ToHashSet();
        List<Guid> people = assignments
            .Where(assignment => personRoles.Contains(assignment.RoleId))
            .Select(assignment => Guid.TryParse(assignment.ObjectId, out Guid person) ? person : Guid.Empty)
            .Where(person => person != Guid.Empty)
            .Distinct()
            .ToList();
        if (people.Count == 0)
            return [];
        ILookup<Guid, (Guid RoleId, string? ObjectId)> theirs = (await db.ActiveAssignments(now)
                .Where(assignment => people.Contains(assignment.UserId))
                .ToListAsync(cancellationToken))
            .ToLookup(assignment => assignment.UserId, assignment => (assignment.RoleId, assignment.ScopeObjectId));
        return RoleGrants.HeldThrough(assignments, person => theirs[person], rules).ToList();
    }
}
