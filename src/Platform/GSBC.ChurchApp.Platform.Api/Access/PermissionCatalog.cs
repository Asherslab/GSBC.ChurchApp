using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Access;

public sealed class PermissionCatalog(IEnumerable<PermissionSet> sets)
{
    public IReadOnlyDictionary<string, Permission> ByNode { get; } =
        sets.SelectMany(set => set.Permissions).ToDictionary(permission => permission.Node);
}

public sealed record PermissionSet(IReadOnlyList<Permission> Permissions);
