using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

public static class AccountsPermissions
{
    public const string ViewNode = "accounts.view";
    public const string ManageNode = "accounts.manage";
    public const string ReviewAccessNode = "accounts.review.manage";

    public static readonly Permission View =
        new(ViewNode, ScopeKind.Global, "See everyone's name and email", isSensitive: true);

    public static readonly Permission Manage =
        new(ManageNode, ScopeKind.Global, "Invite people, give recovery codes, disable accounts, read the audit log", isSensitive: true);

    public static readonly Permission ReviewAccess =
        new(ReviewAccessNode, ScopeKind.Global, "Make sign-in codes for Apple's app reviewers", isSensitive: true);

    public static readonly IReadOnlyList<Permission> All = [View, Manage, ReviewAccess];
}
