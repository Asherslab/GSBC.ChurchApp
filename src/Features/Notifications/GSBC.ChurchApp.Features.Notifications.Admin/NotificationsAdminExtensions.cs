using GSBC.ChurchApp.Features.Notifications.Client;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Notifications.Admin;

public static class NotificationsAdminExtensions
{
    public static IServiceCollection AddNotificationsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Notifications", typeof(NotificationsAdminExtensions).Assembly,
            [new NavEntry("Announcements", "announcements", AppIcons.Announcements, NotificationsPermissions.Write, Area: NavAreas.ThisWeek,
                Summary: "Tell the church something, on everyone's phone")]));
        return services.AddDestinations(NotificationsDestinations.All).AddHubTile<InboxTile>(NotificationsTiles.Inbox)
            .AddSingleton(provider => new AnnouncementsClient(provider.GetRequiredService<Grpc.Core.ChannelBase>())).AddSingleton<IOfficeWeekSource, AnnouncementsOfficeWeek>()
            .AddPlaceAction(new PlaceAction("Announce it", "send", place => $"announcements/new?opens={Uri.EscapeDataString(place.Path)}", NotificationsPermissions.Write));
    }
}
