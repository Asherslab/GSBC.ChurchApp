using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.People.Contracts;

public static class PeoplePermissions
{
    public const string FamilyViewNode = "people.family.view";
    public const string FamilyEditNode = "people.family.edit";
    public const string SignInCodeNode = "people.family.signin";
    public const string ActForNode = "people.family.act";
    public const string FamiliesManageNode = "people.families.manage";
    public const string SafeguardingViewNode = "people.safeguarding.view";
    public const string SafeguardingEditNode = "people.safeguarding.edit";

    public const string ParentRole = "parent-or-guardian";
    public const string SafeguardingViewerRole = "safeguarding-viewer";
    public const string SafeguardingManagerRole = "safeguarding-manager";

    public static readonly Permission FamilyView =
        new(FamilyViewNode, ScopeKind.Person, "See someone's birth date and family", area: "People");

    public static readonly Permission FamilyEdit =
        new(FamilyEditNode, ScopeKind.Person, "Change someone's name and birth date, and remove a child who can't sign in themselves", area: "People");

    public static readonly Permission SignInCode =
        new(SignInCodeNode, ScopeKind.Person, "Make someone a sign-in code for their own phone", area: "People");

    public static readonly Permission ActFor =
        new(ActForNode, ScopeKind.Person, "Act for this young person as their parent: answer, swap and reply for them, shown as the parent", area: "People");

    public static readonly Permission FamiliesManage =
        new(FamiliesManageNode, ScopeKind.Global, "Link and unlink anyone's family, and add children for a parent", isSensitive: true, area: "People");

    public static readonly Permission SafeguardingView =
        new(SafeguardingViewNode, ScopeKind.Person, "See someone's blue card and Safe Ministry training, and who's flagged", isSensitive: true, area: "People");

    public static readonly Permission SafeguardingEdit =
        new(SafeguardingEditNode, ScopeKind.Person, "Record someone's blue card and Safe Ministry training", isSensitive: true, area: "People");

    public static readonly IReadOnlyList<Permission> All = [FamilyView, FamilyEdit, SignInCode, ActFor, FamiliesManage, SafeguardingView, SafeguardingEdit];
}
