using GSBC.ChurchApp.Features.Notifications.Client;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Notifications.Admin;

public static class NotificationsAdminExtensions
{
    public static IServiceCollection AddNotificationsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Notifications", typeof(NotificationsAdminExtensions).Assembly, [], [NotificationsFeeds.Announcements]));
        return services.AddDestinations(NotificationsDestinations.All).AddHubTile<InboxTile>(NotificationsTiles.Inbox)
            .AddSingleton(provider => new AnnouncementsClient(provider.GetRequiredService<Grpc.Core.ChannelBase>())).AddSingleton<IOfficeWeekSource, AnnouncementsOfficeWeek>();
    }
}
