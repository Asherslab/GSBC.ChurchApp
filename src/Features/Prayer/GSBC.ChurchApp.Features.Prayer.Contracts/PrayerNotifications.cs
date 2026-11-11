using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Prayer.Contracts;

public static class PrayerNotifications
{
    public static readonly NotificationKind NewRequest = new(
        "prayer.new", "Someone sends a prayer request", "Prayer", Waits: true, For: PrayerPermissions.Read, Opens: new("/prayer/requests/{id}"));

    public static readonly NotificationKind WaitingForWall = new(
        "prayer.wall.waiting", "A prayer request asks to be on the prayer wall", "Prayer", Waits: true, For: PrayerPermissions.Wall, Opens: new("/prayer/approve"));

    public static readonly NotificationKind PrayedFor = new(
        "prayer.prayed", "Someone prays for your prayer request", "Prayer", Opens: new("/prayer/yours/{id}"));

    public static readonly NotificationKind Answered = new(
        "prayer.answered", "The pastoral team marks your prayer request answered", "Prayer", Opens: new("/prayer/yours/{id}"));

    public static readonly IReadOnlyList<NotificationKind> All = [NewRequest, WaitingForWall, PrayedFor, Answered];
}
