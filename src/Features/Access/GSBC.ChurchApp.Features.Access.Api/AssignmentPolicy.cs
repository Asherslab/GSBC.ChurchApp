using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed record Assigner(GrantSet Grants, bool ThroughATool, IReadOnlyCollection<Assignment> Holds);

internal sealed class AssignmentPolicy(IScopeHierarchy hierarchy, PermissionCatalog catalog)
{
    private const string ToolsCannotChangeRoles = "A tool can't give or take away roles. Do it in the admin panel.";

    public async Task<string?> RefusalAsync(
        Assigner actor, RoleRecord role, string? objectId, IReadOnlyDictionary<Guid, RoleRecord> roles, CancellationToken cancellationToken)
    {
        if (actor.ThroughATool)
            return ToolsCannotChangeRoles;

        ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind);
        if (!actor.Grants.IsAllowed(AccessPermissions.EditAssignments, objectId) && !await HoldsAssignerRoleAsync(actor, role, kind, objectId, roles, cancellationToken))
            return NotAnAssigner(role, roles);

        List<string> patterns = RoleGrants.PatternsOf(roles[role.Id], roles);
        List<string> everywhere = RoleGrants.EverywhereOf(roles[role.Id], roles);
        List<Permission> notHeld = [];
        foreach (string? scope in await RoleGrants.ScopesAsync(role, kind, objectId, hierarchy, cancellationToken))
            notHeld.AddRange(RoleGrants.NotHeld(actor.Grants, patterns, kind, scope, catalog, everywhere));

        return notHeld.Count == 0
            ? null
            : $"You can't give the {role.Name} role, because it lets people do things you can't do yourself: " +
              $"{string.Join(", ", notHeld.Select(permission => permission.Description).Distinct())}.";
    }

    public static string? LastOwner(RoleRecord role, IReadOnlyCollection<Assignment> removing, IEnumerable<Assignment> activeOwners, DateTime now)
    {
        if (role.SystemKey != AccessService.OwnerKey || removing.All(assignment => assignment.ExpiresAt <= now))
            return null;

        HashSet<Guid> removingIds = removing.Select(assignment => assignment.Id).ToHashSet();
        return activeOwners.Any(other => other.RoleId == role.Id && !removingIds.Contains(other.Id))
            ? null
            : "This is the church's last Owner. Make someone else an Owner first.";
    }

    public static bool OnlyOwner(IReadOnlyCollection<Assignment> activeOwners, Guid userId) =>
        activeOwners.Count > 0 && activeOwners.All(owner => owner.UserId == userId);

    private async Task<bool> HoldsAssignerRoleAsync(
        Assigner actor, RoleRecord role, ScopeKind kind, string? objectId, IReadOnlyDictionary<Guid, RoleRecord> roles, CancellationToken cancellationToken)
    {
        HashSet<Guid> assignerIds = role.Assigners.Select(assigner => assigner.AssignerRoleId).ToHashSet();
        List<(Assignment Assignment, RoleRecord Role)> held = actor.Holds
            .Where(assignment => assignerIds.Contains(assignment.RoleId) && roles.ContainsKey(assignment.RoleId))
            .Select(assignment => (assignment, roles[assignment.RoleId]))
            .ToList();
        if (held.Count == 0)
            return false;

        List<string> above = objectId is null || kind == ScopeKind.Global ? [] : await AncestorsAsync(kind, objectId, cancellationToken);
        return held.Any(hold =>
            hold.Role.ScopeKind == ScopeKind.Global.Name
            || (objectId is not null && hold.Assignment.ScopeObjectId == objectId)
            || (hold.Role.AppliesToChildren && hold.Assignment.ScopeObjectId is { } heldAt && above.Contains(heldAt)));
    }

    private async Task<List<string>> AncestorsAsync(ScopeKind kind, string objectId, CancellationToken cancellationToken)
    {
        List<string> above = [];
        for (string? next = await hierarchy.ParentOfAsync(kind, objectId, cancellationToken);
             next is not null && next != objectId && !above.Contains(next);
             next = await hierarchy.ParentOfAsync(kind, next, cancellationToken))
            above.Add(next);
        return above;
    }

    private static string NotAnAssigner(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles)
    {
        List<string> names = role.Assigners
            .Select(assigner => roles.GetValueOrDefault(assigner.AssignerRoleId)?.Name)
            .OfType<string>()
            .Order(StringComparer.CurrentCulture)
            .ToList();

        return names.Count == 0
            ? $"Only people who manage access can give the {role.Name} role."
            : $"Only someone with the {JoinOr(names)} role can give the {role.Name} role. Ask one of them.";
    }

    private static string JoinOr(List<string> names) =>
        names.Count == 1 ? names[0] : $"{string.Join(", ", names[..^1])} or {names[^1]}";
}
