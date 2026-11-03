using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Notifications.Contracts;

public static class AnnouncementNotifications
{
    public static readonly NotificationKind Announcement = new(
        "notifications.announcement", "Announcements from the church", "Announcements",
        MayBeUrgent: true, Pacing: new NotificationPacing(3, TimeSpan.FromHours(1)));

    public static readonly NotificationKind WaitingToSend = new(
        "notifications.announcement.waiting", "An announcement is waiting for you to send", "Announcements",
        Waits: true, For: NotificationsPermissions.Send, InAdminPanel: true);

    public static readonly IReadOnlyList<NotificationKind> All = [Announcement, WaitingToSend];
}
