using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

public static class AccountsPermissions
{
    public const string ManageNode = "accounts.manage";

    public static readonly Permission Manage =
        new(ManageNode, ScopeKind.Global, "Invite people, and manage anyone's passkeys and sessions");

    public static readonly IReadOnlyList<Permission> All = [Manage];
}
