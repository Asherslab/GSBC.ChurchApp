using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Api;

internal static class RoleGrants
{
    public const string AnyObject = "~any";

    public static IEnumerable<Grant> At(IEnumerable<string> patterns, ScopeKind kind, string? objectId) =>
        patterns
            .Select(text => PermissionPattern.TryParse(text, out PermissionPattern? pattern) ? pattern : null)
            .OfType<PermissionPattern>()
            .Select(pattern => new Grant(pattern, kind == ScopeKind.Global ? Scope.Global : Scope.Of(kind, objectId ?? AnyObject)));

    public static IEnumerable<Permission> Abilities(
        IEnumerable<string> rolePatterns, ScopeKind kind, string? objectId, PermissionCatalog catalog, IEnumerable<string>? everywhere = null)
    {
        GrantSet role = new([.. At(rolePatterns, kind, objectId), .. At(everywhere ?? [], ScopeKind.Global, null)]);
        string checkedObject = objectId ?? AnyObject;
        return catalog.ByNode.Values
            .Where(permission => role.IsAllowed(permission, permission.ScopeKind == ScopeKind.Global ? null : checkedObject))
            .OrderBy(permission => permission.Node, StringComparer.Ordinal);
    }

    public static List<Permission> NotHeld(
        GrantSet actor, IEnumerable<string> rolePatterns, ScopeKind kind, string? objectId, PermissionCatalog catalog, IEnumerable<string>? everywhere = null)
    {
        string checkedObject = objectId ?? AnyObject;
        return Abilities(rolePatterns, kind, objectId, catalog, everywhere)
            .Where(permission => !actor.IsAllowed(permission, permission.ScopeKind == ScopeKind.Global ? null : checkedObject))
            .ToList();
    }

    public static List<string> PatternsOf(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles) => Of(role, roles, everywhere: false);

    public static List<string> EverywhereOf(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles) => Of(role, roles, everywhere: true);

    private static List<string> Of(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles, bool everywhere) =>
        role.Includes
            .Select(include => roles.GetValueOrDefault(include.IncludedRoleId))
            .OfType<RoleRecord>()
            .Prepend(role)
            .SelectMany(each => each.Patterns.Where(pattern => pattern.Everywhere == everywhere).Select(pattern => pattern.Pattern))
            .ToList();

    public static async Task<IReadOnlyList<string?>> ScopesAsync(
        RoleRecord role, ScopeKind kind, string? objectId, IScopeHierarchy hierarchy, CancellationToken cancellationToken) =>
        role.AppliesToChildren && kind != ScopeKind.Global && objectId is not null
            ? [objectId, .. await hierarchy.ChildrenOfAsync(kind, objectId, cancellationToken)]
            : [objectId];

    public static IEnumerable<Grant> MembershipOf(RoleRecord role) =>
        role.Includes.Select(include => include.IncludedRoleId).Prepend(role.Id)
            .Select(roleId => new Grant(PermissionPattern.Parse(RoleMembership.NodeOf(roleId)), Scope.Global));

    public static async Task<GrantSet> CompileAsync(
        IEnumerable<(Guid RoleId, string? ObjectId)> assignments, IReadOnlyDictionary<Guid, RoleRecord> roles, IScopeHierarchy hierarchy,
        CancellationToken cancellationToken)
    {
        List<Grant> grants = [];
        foreach ((Guid roleId, string? objectId) in assignments)
        {
            if (!roles.TryGetValue(roleId, out RoleRecord? role) || !ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind))
                continue;
            grants.AddRange(MembershipOf(role));
            List<string> patterns = PatternsOf(role, roles);
            foreach (string? scope in await ScopesAsync(role, kind, objectId, hierarchy, cancellationToken))
                grants.AddRange(At(patterns, kind, scope));
            grants.AddRange(At(EverywhereOf(role, roles), ScopeKind.Global, null));
        }
        return new GrantSet(grants);
    }
}
