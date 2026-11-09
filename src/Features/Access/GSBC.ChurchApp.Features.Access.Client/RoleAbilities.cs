using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Client.People;

namespace GSBC.ChurchApp.Features.Access.Client;

public static class RoleAbilities
{
    public static PlaceRoleView View(PlaceRole role, IReadOnlyList<PermissionInfo> catalog) =>
        new(role.Id, role.Name, role.Description, Groups(role.AbilityNodes, catalog), role.Refusal);

    public static IReadOnlyList<AbilityGroup> Groups(IEnumerable<string> nodes, IReadOnlyList<PermissionInfo> catalog)
    {
        List<PermissionInfo> abilities = nodes
            .Select(node => catalog.FirstOrDefault(permission => permission.Node == node))
            .OfType<PermissionInfo>()
            .ToList();
        return abilities.GroupBy(permission => permission.Area)
            .OrderByDescending(area => area.Count())
            .Select(area => new AbilityGroup(area.Key, area.Select(permission => permission.Description).ToList()))
            .ToList();
    }
}
