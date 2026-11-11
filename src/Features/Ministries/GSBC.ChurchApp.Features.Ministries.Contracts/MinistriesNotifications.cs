using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class MinistriesNotifications
{
    public static readonly NotificationKind Important = new(
        "ministries.important", "An important message from a ministry you're part of", "Ministries", Opens: new("/ministries/{id}/news", "/ministries/{id}"));

    public static readonly NotificationKind Asked = new(
        "ministries.asked", "Someone asks to join a ministry you lead", "Ministries", Waits: true, Opens: new("/ministries/{id}/people"));

    public static readonly NotificationKind Answered = new(
        "ministries.answered", "A ministry said yes", "Ministries", Opens: new("/ministries/{id}"));

    public static readonly IReadOnlyList<NotificationKind> All = [Important, Asked, Answered];
}
