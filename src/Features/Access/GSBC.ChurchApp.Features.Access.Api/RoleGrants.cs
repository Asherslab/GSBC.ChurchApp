using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Api;

internal static class RoleGrants
{
    private const string AnyObject = "~any";

    public static IEnumerable<Grant> At(IEnumerable<string> patterns, ScopeKind kind, string? objectId) =>
        patterns
            .Select(text => PermissionPattern.TryParse(text, out PermissionPattern? pattern) ? pattern : null)
            .OfType<PermissionPattern>()
            .Select(pattern => new Grant(pattern, kind == ScopeKind.Global ? Scope.Global : Scope.Of(kind, objectId ?? AnyObject)));

    public static List<string> NotHeld(GrantSet actor, IEnumerable<string> rolePatterns, ScopeKind kind, string? objectId, PermissionCatalog catalog)
    {
        GrantSet role = new(At(rolePatterns, kind, objectId).ToList());
        string checkedObject = objectId ?? AnyObject;
        return catalog.ByNode.Values
            .Where(permission =>
            {
                string? target = permission.ScopeKind == ScopeKind.Global ? null : checkedObject;
                return role.IsAllowed(permission, target) && !actor.IsAllowed(permission, target);
            })
            .Select(permission => permission.Node)
            .Order()
            .ToList();
    }
}
