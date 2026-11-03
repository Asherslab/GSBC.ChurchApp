namespace GSBC.ChurchApp.Platform.Permissions;

public static class RoleMembership
{
    public const string Prefix = "roles.held.";

    public static string NodeOf(Guid roleId) => Prefix + roleId.ToString("N");

    public static Permission Of(Guid roleId) => new(NodeOf(roleId), ScopeKind.Global, "Holds this role");
}
