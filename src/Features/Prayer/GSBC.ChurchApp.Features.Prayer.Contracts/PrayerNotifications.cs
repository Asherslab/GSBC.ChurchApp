using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Prayer.Contracts;

public static class PrayerNotifications
{
    public static readonly NotificationKind NewRequest = new(
        "prayer.new", "Someone sends a prayer request", "Prayer", Waits: true, For: PrayerPermissions.Read, InAdminPanel: true);

    public static readonly NotificationKind WaitingForWall = new(
        "prayer.wall.waiting", "A prayer request asks to be on the prayer wall", "Prayer", Waits: true, For: PrayerPermissions.Wall, InAdminPanel: true);

    public static readonly NotificationKind PrayedFor = new(
        "prayer.prayed", "Someone prays for your prayer request", "Prayer");

    public static readonly NotificationKind Answered = new(
        "prayer.answered", "The pastoral team marks your prayer request answered", "Prayer");

    public static readonly IReadOnlyList<NotificationKind> All = [NewRequest, WaitingForWall, PrayedFor, Answered];
}
