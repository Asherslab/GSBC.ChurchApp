using GSBC.ChurchApp.Platform.Client.People;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Admin;

public static class AccessFeatures
{
    public const string Owner = "Owner";
    private const string Other = "Other";

    public static IEnumerable<IGrouping<string, PlaceRoleView>> Grouped(IReadOnlyList<PlaceRoleView> roles, IReadOnlyDictionary<string, string?> features) =>
        roles.GroupBy(role => features.GetValueOrDefault(role.Id) ?? Other)
            .OrderBy(feature => RoleFeatures.InOrder.Contains(feature.Key) ? RoleFeatures.InOrder.ToList().IndexOf(feature.Key) : RoleFeatures.InOrder.Count);
}
