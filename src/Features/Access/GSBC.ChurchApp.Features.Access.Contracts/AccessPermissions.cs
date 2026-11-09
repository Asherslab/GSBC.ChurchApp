using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Contracts;

public static class AccessPermissions
{
    public const string ViewNode = "access.view";
    public const string EditAssignmentsNode = "access.assignments.edit";

    public static readonly Permission View =
        new(ViewNode, ScopeKind.Global, "See roles and what each person can do", isSensitive: true, area: "People");

    public static readonly Permission EditAssignments =
        new(EditAssignmentsNode, ScopeKind.Ministry, "Give people roles and take them away", isSensitive: true, area: "People");

    public static readonly IReadOnlyList<Permission> All = [View, EditAssignments];
}
