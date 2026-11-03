using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Api.Push;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Notifications.Api;

public static class NotificationsApiExtensions
{
    public static IServiceCollection AddNotificationsApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNotificationsData();
        services.Configure<ApnsOptions>(configuration.GetSection(ApnsOptions.Section));
        services.AddSingleton<NotificationKindCatalog>();
        services.AddSingleton<PushSignal>();
        services.AddScoped<NotificationRaiser>();
        services.AddScoped<INotifications>(provider => provider.GetRequiredService<NotificationRaiser>());
        services.AddScoped<AnnouncementsService>();
        services.AddScoped<PushDispatcher>();

        ApnsOptions apns = configuration.GetSection(ApnsOptions.Section).Get<ApnsOptions>() ?? new ApnsOptions();
        if (apns.IsConfigured)
            services.AddHttpClient<IPushTransport, ApnsTransport>();
        else
            services.AddSingleton<IPushTransport, PushOff>();

        services.AddHostedService<PushDelivery>();
        services.AddHostedService<SyncNudger>();
        services.AddHostedService<KindsFeed>();
        services.AddHostedService<AnnouncementScheduler>();

        services.AddNotificationKinds(AnnouncementNotifications.All);
        services.AddMeasures(NotificationsMeasures.All);
        services.AddPermissions(NotificationsPermissions.All);
        services.AddSystemRole(new SystemRole(NotificationsPermissions.AnnouncerRole, "Announcer",
            "Writes and sends announcements to the church", ScopeKind.Global, ["notifications.announcements.*"]));
        services.AddFeed(new FeedDefinition(NotificationsFeeds.Inbox, FeedAudience.OwnRecords));
        services.AddFeed(new FeedDefinition(NotificationsFeeds.Preferences, FeedAudience.OwnRecords));
        services.AddFeed(new FeedDefinition(NotificationsFeeds.Public, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(NotificationsFeeds.Kinds, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(NotificationsFeeds.Announcements, FeedAudience.Holding(NotificationsPermissions.Write)));
        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<NotificationsService>();
        endpoints.MapGrpcService<AnnouncementsService>();
        return endpoints;
    }
}
