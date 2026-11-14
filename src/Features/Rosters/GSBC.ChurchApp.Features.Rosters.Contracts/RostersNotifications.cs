using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Rosters.Contracts;

public static class RostersNotifications
{
    private static readonly Permission ParentOf =
        new("people.family.act", ScopeKind.Person, "Act for this young person as their parent", area: "People");

    private static readonly IReadOnlyList<Permission> OnARoster = [RostersPermissions.Reply, ParentOf];

    public static readonly NotificationKind PutOn = new(
        "rosters.on", "You're put on a roster, or taken off it", "Rosters", For: OnARoster, Opens: new("/ministries/{id}/roster"));

    public static readonly NotificationKind Reminder = new(
        "rosters.reminder", "A reminder before a date you're on", "Rosters", For: OnARoster, Opens: new("/ministries/{id}/roster"));

    public static readonly NotificationKind Nudge = new(
        "rosters.nudge", "A date on a roster still waiting for your answer", "Rosters", Waits: true, For: OnARoster, Opens: new("/ministries/{id}/roster"));

    public static readonly NotificationKind Swaps = new(
        "rosters.swaps", "Someone asks you to take their place on a roster, or a swap you asked for is settled", "Rosters", Waits: true, For: OnARoster,
        Opens: new("/ministries/{id}/roster"));

    public static readonly NotificationKind ForLeaders = new(
        "rosters.leaders", "On a roster you lead: someone can't do a date, a swap, or someone away on a date they're on", "Rosters", Waits: true,
        For: [RostersPermissions.Edit], Opens: new("/ministries/{id}/roster"));

    public static readonly IReadOnlyList<NotificationKind> All = [PutOn, Reminder, Nudge, Swaps, ForLeaders];
}
