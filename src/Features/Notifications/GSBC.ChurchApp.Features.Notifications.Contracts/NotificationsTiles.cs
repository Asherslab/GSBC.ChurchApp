using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Notifications.Contracts;

public static class NotificationsTiles
{
    public static readonly HubTileKind Inbox = new("hub.notifications", "Notifications",
        "How many notifications are unread, and the newest one", "Notifications",
        new TileDefaults("All caught up", "Notifications", Icon: "bell", Link: NotificationsDestinations.Inbox.Path,
            TitleLabel: "Title when nothing is unread", TextLabel: "Words under it when nothing is unread"));

    public static SurfaceSet Surfaces => new([Inbox.Surface]);
}
