using GSBC.ChurchApp.Features.Notifications.Client;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Notifications.App;

public static class NotificationsAppExtensions
{
    public static IServiceCollection AddNotificationsApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Notifications", typeof(NotificationsAppExtensions).Assembly,
            [new NavEntry("Notification settings", "notifications/settings", "bell")],
            [NotificationsFeeds.Public, NotificationsFeeds.Kinds],
            [NotificationsFeeds.Inbox, NotificationsFeeds.Preferences]));
        return services.AddNotificationsOnPhone();
    }
}
