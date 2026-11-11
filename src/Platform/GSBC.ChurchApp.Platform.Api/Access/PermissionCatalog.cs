using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Access;

public sealed class PermissionCatalog(IEnumerable<PermissionSet> sets)
{
    public IReadOnlyDictionary<string, Permission> ByNode { get; } =
        sets.SelectMany(set => set.Permissions).ToDictionary(permission => permission.Node);

    public IReadOnlyList<Permission> OpeningAdminAreas => field ??= ByNode.Values.Where(permission => permission.OpensAdminArea).ToList();

    public IReadOnlyList<Permission> Sensitive => field ??= ByNode.Values.Where(permission => permission.IsSensitive).ToList();
}

public sealed record PermissionSet(IReadOnlyList<Permission> Permissions);
