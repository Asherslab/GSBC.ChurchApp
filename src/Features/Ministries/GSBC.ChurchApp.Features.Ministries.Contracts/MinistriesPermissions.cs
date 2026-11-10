using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class MinistriesPermissions
{
    public const string TeamViewNode = "ministries.team.view";
    public const string AttendeesViewNode = "ministries.attendees.view";
    public const string ParentsViewNode = "ministries.parents.view";
    public const string PostNode = "ministries.posts.write";
    public const string PeopleNode = "ministries.people.manage";
    public const string SettingsNode = "ministries.settings.edit";
    public const string CreateNode = "ministries.create";
    public const string PlanLifeGroupsNode = "ministries.lifegroups.plan";

    public const string LeaderRole = "ministry-leader";
    public const string AdminRole = "ministry-admin";
    public const string TeamRole = "ministry-team";
    public const string AttendeeRole = "ministry-attendee";
    public const string AttendeesParentRole = "ministry-attendees-parent";
    public const string ManagerRole = "ministries-manager";
    public const string LifeGroupsPlannerRole = "life-groups-planner";

    public static readonly Permission TeamView =
        new(TeamViewNode, ScopeKind.Ministry, "See what a ministry shares with its team", area: "Ministries");

    public static readonly Permission AttendeesView =
        new(AttendeesViewNode, ScopeKind.Ministry, "See what a ministry shares with its attendees and their parents", area: "Ministries");

    public static readonly Permission ParentsView =
        new(ParentsViewNode, ScopeKind.Ministry, "See what a ministry shares with its attendees' parents", area: "Ministries");

    public static readonly Permission Post =
        new(PostNode, ScopeKind.Ministry, "Post news and add files and links to a ministry", area: "Ministries");

    public static readonly Permission People =
        new(PeopleNode, ScopeKind.Ministry, "Add and remove a ministry's team and attendees, answer asks to join and link their families", area: "Ministries");

    public static readonly Permission Settings =
        new(SettingsNode, ScopeKind.Ministry, "Change a ministry's details, joining and calendars, and add and delete the ministries under it", area: "Ministries");

    public static readonly Permission Create =
        new(CreateNode, ScopeKind.Global, "Create and delete the church's top-level ministries", area: "Ministries");

    public static readonly Permission PlanLifeGroups =
        new(PlanLifeGroupsNode, ScopeKind.Global, "Plan the Life Groups: their Sundays, their time and when they show in the app", area: "Ministries");

    public static readonly IReadOnlyList<Permission> All = [TeamView, AttendeesView, ParentsView, Post, People, Settings, Create, PlanLifeGroups];
}
