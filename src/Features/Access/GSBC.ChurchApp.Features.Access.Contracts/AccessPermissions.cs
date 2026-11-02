using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Contracts;

public static class AccessPermissions
{
    public const string ViewNode = "access.view";
    public const string EditRolesNode = "access.roles.edit";
    public const string EditAssignmentsNode = "access.assignments.edit";

    public static readonly Permission View =
        new(ViewNode, ScopeKind.Global, "See roles and what each person can do", isSensitive: true);

    public static readonly Permission EditRoles =
        new(EditRolesNode, ScopeKind.Global, "Make and change roles", isSensitive: true);

    public static readonly Permission EditAssignments =
        new(EditAssignmentsNode, ScopeKind.Ministry, "Give people roles and take them away", isSensitive: true);

    public static readonly IReadOnlyList<Permission> All = [View, EditRoles, EditAssignments];
}
