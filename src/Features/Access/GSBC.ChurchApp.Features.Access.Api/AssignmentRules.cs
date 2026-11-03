using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AssignmentRules(
    AccessDbContext db, IAccessEvaluator evaluator, PermissionCatalog catalog, IScopeHierarchy hierarchy, ToolCallers tools, TimeProvider time)
{
    private const string ToolsCannotChangeRoles = "A tool can't give or take away roles. Do it in the admin panel.";

    public static string? ScopeProblem(ScopeKind kind, string? objectId) =>
        (kind == ScopeKind.Global, objectId is null) switch
        {
            (true, false) => "This role applies everywhere, so it isn't given for one place.",
            (false, true) => $"Choose which {kind.Name} this role is for.",
            _ => null,
        };

    public async Task<string?> RefusalAsync(Guid actorId, RoleRecord role, string? objectId, CancellationToken cancellationToken)
    {
        if (tools.ActsThroughATool(actorId))
            return ToolsCannotChangeRoles;

        ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind);
        GrantSet actor = await evaluator.GrantsForAsync(actorId, cancellationToken);
        if (!actor.IsAllowed(AccessPermissions.EditAssignments, objectId) && !await HoldsAssignerRoleAsync(actorId, role, kind, objectId, cancellationToken))
            return await NotAnAssignerAsync(role, cancellationToken);

        Dictionary<Guid, RoleRecord> roles = await db.LoadRolesWithIncludesAsync([role.Id], cancellationToken);
        List<string> patterns = RoleGrants.PatternsOf(roles[role.Id], roles);
        List<Permission> notHeld = [];
        foreach (string? scope in await RoleGrants.ScopesAsync(role, kind, objectId, hierarchy, cancellationToken))
            notHeld.AddRange(RoleGrants.NotHeld(actor, patterns, kind, scope, catalog));

        return notHeld.Count == 0
            ? null
            : $"You can't give the {role.Name} role, because it lets people do things you can't do yourself: " +
              $"{string.Join(", ", notHeld.Select(permission => permission.Description).Distinct())}.";
    }

    public async Task<string?> LastOwnerAsync(RoleRecord role, IReadOnlyCollection<Assignment> removing, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        if (role.SystemKey != AccessService.OwnerKey || removing.All(assignment => assignment.ExpiresAt <= now))
            return null;

        List<Guid> removingIds = removing.Select(assignment => assignment.Id).ToList();
        bool anotherRemains = await db.ActiveAssignments(now)
            .AnyAsync(other => other.RoleId == role.Id && !removingIds.Contains(other.Id), cancellationToken);
        return anotherRemains ? null : "This is the church's last Owner. Make someone else an Owner first.";
    }

    private async Task<bool> HoldsAssignerRoleAsync(Guid actorId, RoleRecord role, ScopeKind kind, string? objectId, CancellationToken cancellationToken)
    {
        if (role.Assigners.Count == 0)
            return false;

        List<Guid> assignerIds = role.Assigners.Select(assigner => assigner.AssignerRoleId).ToList();
        List<Assignment> held = await db.ActiveAssignments(time.GetUtcNow().UtcDateTime).Include(assignment => assignment.Role)
            .Where(assignment => assignment.UserId == actorId && assignerIds.Contains(assignment.RoleId))
            .ToListAsync(cancellationToken);
        string? parent = objectId is null || kind == ScopeKind.Global ? null : await hierarchy.ParentOfAsync(kind, objectId, cancellationToken);

        return held.Any(assignment =>
            assignment.Role!.ScopeKind == ScopeKind.Global.Name
            || (objectId is not null && assignment.ScopeObjectId == objectId)
            || (parent is not null && assignment.ScopeObjectId == parent && assignment.Role.AppliesToChildren));
    }

    private async Task<string> NotAnAssignerAsync(RoleRecord role, CancellationToken cancellationToken)
    {
        List<Guid> assignerIds = role.Assigners.Select(assigner => assigner.AssignerRoleId).ToList();
        List<string> names = await db.Roles.AsNoTracking().Where(other => assignerIds.Contains(other.Id))
            .OrderBy(other => other.Name).Select(other => other.Name).ToListAsync(cancellationToken);

        return names.Count == 0
            ? $"Only people who manage access can give the {role.Name} role."
            : $"Only someone with the {JoinOr(names)} role can give the {role.Name} role. Ask one of them.";
    }

    private static string JoinOr(List<string> names) =>
        names.Count == 1 ? names[0] : $"{string.Join(", ", names[..^1])} or {names[^1]}";
}
