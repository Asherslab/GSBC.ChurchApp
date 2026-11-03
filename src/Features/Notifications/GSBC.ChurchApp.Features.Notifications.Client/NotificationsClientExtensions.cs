using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Client.Analytics;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Notifications.Client;

public static class NotificationsClientExtensions
{
    public static IServiceCollection AddNotificationsOnPhone(this IServiceCollection services) =>
        services
            .AddDestinations(NotificationsDestinations.All)
            .AddSingleton<Inbox>()
            .AddSingleton<IInboxCount>(provider => provider.GetRequiredService<Inbox>())
            .AddSingleton<NotificationPreferences>()
            .AddSingleton<PushRegistration>()
            .AddSingleton<IClientStartupTask>(provider => provider.GetRequiredService<PushRegistration>())
            .AddSingleton<IWeeklyState, PushSetting>();

    public static IServiceCollection AddAnnouncements(this IServiceCollection services) =>
        services.AddDestinations(NotificationsDestinations.All).AddSingleton(provider => new AnnouncementsClient(provider.GetRequiredService<Grpc.Core.ChannelBase>()));
}
