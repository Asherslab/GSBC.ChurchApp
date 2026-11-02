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

    public static IEnumerable<Permission> Abilities(IEnumerable<string> rolePatterns, ScopeKind kind, string? objectId, PermissionCatalog catalog)
    {
        GrantSet role = new(At(rolePatterns, kind, objectId).ToList());
        string checkedObject = objectId ?? AnyObject;
        return catalog.ByNode.Values
            .Where(permission => role.IsAllowed(permission, permission.ScopeKind == ScopeKind.Global ? null : checkedObject))
            .OrderBy(permission => permission.Node, StringComparer.Ordinal);
    }

    public static List<Permission> NotHeld(GrantSet actor, IEnumerable<string> rolePatterns, ScopeKind kind, string? objectId, PermissionCatalog catalog)
    {
        string checkedObject = objectId ?? AnyObject;
        return Abilities(rolePatterns, kind, objectId, catalog)
            .Where(permission => !actor.IsAllowed(permission, permission.ScopeKind == ScopeKind.Global ? null : checkedObject))
            .ToList();
    }

    public static List<string> PatternsOf(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles) =>
        role.Patterns.Select(pattern => pattern.Pattern)
            .Concat(role.Includes
                .Select(include => roles.GetValueOrDefault(include.IncludedRoleId))
                .OfType<RoleRecord>()
                .SelectMany(included => included.Patterns.Select(pattern => pattern.Pattern)))
            .ToList();

    public static async Task<IReadOnlyList<string?>> ScopesAsync(
        RoleRecord role, ScopeKind kind, string? objectId, IScopeHierarchy hierarchy, CancellationToken cancellationToken) =>
        role.AppliesToChildren && kind != ScopeKind.Global && objectId is not null
            ? [objectId, .. await hierarchy.ChildrenOfAsync(kind, objectId, cancellationToken)]
            : [objectId];

    public static async Task<GrantSet> CompileAsync(
        IEnumerable<(Guid RoleId, string? ObjectId)> assignments, IReadOnlyDictionary<Guid, RoleRecord> roles, IScopeHierarchy hierarchy,
        CancellationToken cancellationToken)
    {
        List<Grant> grants = [];
        foreach ((Guid roleId, string? objectId) in assignments)
        {
            if (!roles.TryGetValue(roleId, out RoleRecord? role) || !ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind))
                continue;
            List<string> patterns = PatternsOf(role, roles);
            foreach (string? scope in await ScopesAsync(role, kind, objectId, hierarchy, cancellationToken))
                grants.AddRange(At(patterns, kind, scope));
        }
        return new GrantSet(grants);
    }
}
