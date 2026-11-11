using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Prayer.Contracts;

public static class PrayerNotifications
{
    public static readonly NotificationKind NewRequest = new(
        "prayer.new", "Someone sends a prayer request", "Prayer", Waits: true, For: PrayerPermissions.Read, Opens: new("/prayer/requests/{id}"));

    public static readonly NotificationKind WaitingForWall = new(
        "prayer.wall.waiting", "A prayer request asks to be on the prayer wall", "Prayer", Waits: true, For: PrayerPermissions.Wall, Opens: new("/prayer/approve"));

    public static readonly DigestSchedule PrayedForSchedule = new(
        [TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(45), TimeSpan.FromHours(1), TimeSpan.FromHours(3), TimeSpan.FromHours(6)],
        new TimeOnly(12, 0));

    public static readonly NotificationKind PrayedFor = new(
        "prayer.prayed", "Someone prays for your prayer request", "Prayer", Opens: new("/prayer/yours/{id}"),
        Digest: new NotificationDigest("Someone prayed for your request", "{0} people prayed for your request", "Your request is on the prayer wall.",
            PrayedForSchedule, ChangedBy: PrayerPermissions.Settings));

    public static readonly NotificationKind Answered = new(
        "prayer.answered", "The pastoral team marks your prayer request answered", "Prayer", Opens: new("/prayer/yours/{id}"));

    public static readonly IReadOnlyList<NotificationKind> All = [NewRequest, WaitingForWall, PrayedFor, Answered];
}
