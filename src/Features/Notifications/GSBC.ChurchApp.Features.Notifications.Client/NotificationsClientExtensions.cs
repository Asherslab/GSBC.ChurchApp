using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Notifications.Client;

public static class NotificationsClientExtensions
{
    public static IServiceCollection AddNotificationsOnPhone(this IServiceCollection services) =>
        services
            .AddSingleton<Inbox>()
            .AddSingleton<IInboxCount>(provider => provider.GetRequiredService<Inbox>())
            .AddSingleton<NotificationPreferences>()
            .AddSingleton<PushRegistration>()
            .AddSingleton<IClientStartupTask>(provider => provider.GetRequiredService<PushRegistration>());

    public static IServiceCollection AddAnnouncements(this IServiceCollection services) =>
        services.AddSingleton(provider => new AnnouncementsClient(provider.GetRequiredService<Grpc.Core.ChannelBase>()));
}
