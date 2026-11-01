using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

public static class AccountsPermissions
{
    public static readonly Permission Manage =
        new("accounts.manage", ScopeKind.Global, "Invite people, and manage anyone's passkeys and sessions");

    public static readonly IReadOnlyList<Permission> All = [Manage];
}
