using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class MinistriesNotifications
{
    public static readonly NotificationKind Important = new(
        "ministries.important", "An important message from a ministry you're part of", "Ministries");

    public static readonly NotificationKind Asked = new(
        "ministries.asked", "Someone asks to join a ministry you lead", "Ministries", Waits: true);

    public static readonly NotificationKind Answered = new(
        "ministries.answered", "A ministry said yes", "Ministries");

    public static readonly IReadOnlyList<NotificationKind> All = [Important, Asked, Answered];
}
