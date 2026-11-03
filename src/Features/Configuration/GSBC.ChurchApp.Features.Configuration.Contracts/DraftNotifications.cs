using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public static class DraftNotifications
{
    public static readonly NotificationKind WaitingToPublish = new(
        "config.drafts.waiting", "A draft is waiting for you to publish", "Drafts");

    public static readonly NotificationKind SharedWithYou = new(
        "config.drafts.shared", "Someone shares a draft with you", "Drafts");

    public static readonly IReadOnlyList<NotificationKind> All = [WaitingToPublish, SharedWithYou];
}
