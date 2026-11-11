using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Api.Push;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
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
        services.AddScoped<DigestSchedules>();
        services.AddScoped<DigestSending>();
        services.AddScoped<IPersonData, NotificationsPersonData>();
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
        services.AddHostedService<DigestSender>();

        services.AddNotificationKinds(AnnouncementNotifications.All);
        services.AddMeasures(NotificationsMeasures.All);
        services.AddScoped<IRoleUses, NotificationsRoleUses>();
        services.AddPermissions(NotificationsPermissions.All);
        services.AddDestinations(NotificationsDestinations.All);
        services.AddSurfaces(NotificationsTiles.Surfaces);
        services.AddSystemRole(new SystemRole(NotificationsPermissions.WriterRole, "Announcements Writer",
            "Write announcements, try them on their own phone, and ask for them to be sent", ScopeKind.Global,
            [NotificationsPermissions.WriteNode], Feature: RoleFeatures.Announcements));
        services.AddSystemRole(new SystemRole(NotificationsPermissions.SenderRole, "Announcements Sender",
            "Write announcements, and send and schedule them to everyone", ScopeKind.Global,
            [NotificationsPermissions.WriteNode, NotificationsPermissions.SendNode], Feature: RoleFeatures.Announcements, Replaces: ["announcer"]));
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
