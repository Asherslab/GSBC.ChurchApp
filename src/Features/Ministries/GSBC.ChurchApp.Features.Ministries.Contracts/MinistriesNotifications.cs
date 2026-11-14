using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class MinistriesNotifications
{
    private static readonly Permission ParentOf =
        new("people.family.act", ScopeKind.Person, "Act for this young person as their parent", area: "People");

    public static readonly NotificationKind Important = new(
        "ministries.important", "An important message from a ministry you're part of", "Ministries", For: [MinistriesPermissions.AttendeesView],
        Opens: new("/ministries/{id}/news", "/ministries/{id}"));

    public static readonly NotificationKind Asked = new(
        "ministries.asked", "Someone asks to join a ministry you lead", "Ministries", Waits: true, For: [MinistriesPermissions.People], Opens: new("/ministries/{id}/people"));

    public static readonly NotificationKind Answered = new(
        "ministries.answered", "A ministry said yes", "Ministries", Opens: new("/ministries/{id}"));

    public static readonly NotificationKind ForYourYes = new(
        "ministries.parent", "Your child asks to join a ministry's team", "Ministries", Waits: true, For: [ParentOf], Opens: new("/ministries/{id}"));

    public static readonly NotificationKind YoungJoined = new(
        "ministries.young-joined", "A young person joins a team you lead, with a parent's yes", "Ministries", For: [MinistriesPermissions.People], Opens: new("/ministries/{id}/people"));

    public static readonly IReadOnlyList<NotificationKind> All = [Important, Asked, Answered, ForYourYes, YoungJoined];
}
