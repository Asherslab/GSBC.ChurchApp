using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Notifications.Contracts;

public static class NotificationsTiles
{
    public static readonly HubTileKind Inbox = new("hub.notifications", "Notifications",
        "How many notifications are unread, and the newest one", "Notifications",
        new TileDefaults("All caught up", "Notifications", Icon: "bell", Link: NotificationsDestinations.Inbox.Path,
            TitleLabel: "Title when nothing is unread", TextLabel: "Words under it when nothing is unread"),
        variants:
        [
            new(Count, "Count and newest", "The bell, how many are unread and the newest title", TileVariant.AnySize, (span, _) => TileVariant.Small(span)),
            new(Newest, "Newest in full", "The count above the newest notice and its first lines", TileVariant.AnySize, (span, _) => TileVariant.OneRowWide(span)),
            new(Dots, "Unread dots", "The newest titles with an unread dot each; taller tiles show more", TileVariant.AnySize, (span, _) => TileVariant.Tall(span)),
        ]);

    public const string Count = "count";
    public const string Newest = "newest";
    public const string Dots = "dots";

    public static SurfaceSet Surfaces => new([Inbox.Surface]);
}
