using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.Notifications.Contracts;

public static class NotificationsDestinations
{
    public static readonly DestinationPage Inbox = new("/notifications", "Notifications", AppIcons.Notifications, DestinationAreas.Pages, "The bell, on every tab", Audience.Everyone, Rank: 10);
    public static readonly DestinationPage Settings = new("/notifications/settings", "Notification settings", AppIcons.Settings, DestinationAreas.Pages, "More → Notification settings", Audience.Everyone);

    public static readonly DestinationSet All = DestinationSet.Of(Inbox, Settings);
}
