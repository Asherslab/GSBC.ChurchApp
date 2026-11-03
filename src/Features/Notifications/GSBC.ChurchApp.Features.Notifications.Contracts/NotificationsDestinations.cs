using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Notifications.Contracts;

public static class NotificationsDestinations
{
    public static readonly DestinationPage Inbox = new("/notifications", "Notifications", "bell", DestinationAreas.Pages, "The bell, on every tab", Audience.Everyone, Rank: 10);
    public static readonly DestinationPage Settings = new("/notifications/settings", "Notification settings", "sliders", DestinationAreas.Pages, "More → Notification settings", Audience.Everyone);

    public static readonly DestinationSet All = DestinationSet.Of(Inbox, Settings);
}
