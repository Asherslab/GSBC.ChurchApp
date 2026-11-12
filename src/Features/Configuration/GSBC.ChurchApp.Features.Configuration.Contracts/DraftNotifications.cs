using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public static class DraftNotifications
{
    public static readonly NotificationKind WaitingToPublish = new(
        "config.drafts.waiting", "A draft is waiting for you to publish", "Drafts", Waits: true, For: HubSurfaces.Edit, Opens: new("/previews"));

    public static readonly NotificationKind SharedWithYou = new(
        "config.drafts.shared", "Someone shares a draft with you", "Drafts", Waits: true,
        For: ConfigurationPermissions.PreviewDraft, Opens: new("/previews"));

    public static readonly NotificationKind NotPublished = new(
        "config.drafts.not-published", "A draft you scheduled to publish couldn't be published", "Drafts", Waits: true,
        For: ConfigurationPermissions.PreviewDraft, Opens: new("/previews"));

    public static readonly IReadOnlyList<NotificationKind> All = [WaitingToPublish, SharedWithYou, NotPublished];
}
