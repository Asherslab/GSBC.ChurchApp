using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Rosters.Contracts;

public static class RostersNotifications
{
    public static readonly NotificationKind PutOn = new(
        "rosters.on", "You're put on a roster, or taken off it", "Rosters", Opens: new("/ministries/{id}/roster"));

    public static readonly NotificationKind Reminder = new(
        "rosters.reminder", "A reminder before a date you're on", "Rosters", Opens: new("/ministries/{id}/roster"));

    public static readonly NotificationKind Nudge = new(
        "rosters.nudge", "A date on a roster still waiting for your answer", "Rosters", Waits: true, Opens: new("/ministries/{id}/roster"));

    public static readonly NotificationKind Swaps = new(
        "rosters.swaps", "Someone asks you to take their place on a roster, or a swap you asked for is settled", "Rosters", Waits: true, Opens: new("/ministries/{id}/roster"));

    public static readonly NotificationKind ForLeaders = new(
        "rosters.leaders", "On a roster you lead: someone can't do a date, a swap, or someone away on a date they're on", "Rosters", Waits: true, Opens: new("/ministries/{id}/roster"));

    public static readonly IReadOnlyList<NotificationKind> All = [PutOn, Reminder, Nudge, Swaps, ForLeaders];
}
