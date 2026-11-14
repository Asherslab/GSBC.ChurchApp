using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

public static class OutreachMapsPermissions
{
    public const string ManageNode = "outreach.maps.manage";

    public static readonly Permission Manage =
        new(ManageNode, ScopeKind.Global, "Add, rebuild and delete the areas outreach maps and streets cover", area: "Outreach");

    public static readonly IReadOnlyList<Permission> All = [Manage];
}
