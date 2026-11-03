using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Features.Notifications.Client;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Notifications.Admin;

public static class NotificationsAdminExtensions
{
    public static IServiceCollection AddNotificationsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Notifications", typeof(NotificationsAdminExtensions).Assembly,
            [new NavEntry("Announcements", "announcements", "send", NotificationsPermissions.Write, Group: NavGroups.Content,
                Summary: "Tell the church something, on everyone's phone")]));
        return services.AddAnnouncements().AddSingleton<IOfficeWeekSource, AnnouncementsOfficeWeek>()
            .AddPlaceAction(new PlaceAction("Announce it", "send", place => $"announcements/new?opens={Uri.EscapeDataString(place.Path)}", NotificationsPermissions.Write));
    }
}
