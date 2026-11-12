using GSBC.ChurchApp.Features.Notifications.Client;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Notifications.App;

public static class NotificationsAppExtensions
{
    public static IServiceCollection AddNotificationsApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Notifications", typeof(NotificationsAppExtensions).Assembly,
            [new NavEntry("Notification settings", "notifications/settings", AppIcons.Settings)],
            [NotificationsFeeds.Public, NotificationsFeeds.Kinds],
            [NotificationsFeeds.Inbox, NotificationsFeeds.Preferences]));
        return services
            .AddNotificationsOnPhone()
            .AddSingleton<NotificationsWelcome>()
            .AddSingleton<IOpeningSheet>(provider => provider.GetRequiredService<NotificationsWelcome>());
    }
}
