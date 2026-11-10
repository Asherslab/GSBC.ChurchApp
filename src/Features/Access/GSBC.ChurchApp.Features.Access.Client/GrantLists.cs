using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Client;

internal static class GrantLists
{
    public static GrantSet ToGrantSet(GrantList list) =>
        new(list.Grants
            .Select(grant => PermissionPattern.TryParse(grant.Pattern, out PermissionPattern? pattern) && ScopeKind.TryParse(grant.ScopeKind, out ScopeKind kind)
                ? new Grant(pattern, kind == ScopeKind.Global ? Scope.Global : Scope.Of(kind, grant.ObjectId ?? ""))
                : (Grant?)null)
            .OfType<Grant>()
            .ToList());
}
