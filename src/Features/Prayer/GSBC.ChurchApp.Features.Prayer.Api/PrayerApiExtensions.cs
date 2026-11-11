using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.Api;

public static class PrayerApiExtensions
{
    public static IServiceCollection AddPrayerApi(this IServiceCollection services)
    {
        services.AddPrayerData();
        services.AddScoped<PrayerPublisher>().AddScoped<PrayerTelling>().AddScoped<PrayerRequests>().AddScoped<PastoralCare>().AddScoped<PrayerKeeping>()
            .AddScoped<HandledRequests>();
        services.AddScoped<IPersonData, PrayerPersonData>();
        services.AddHostedService<PrayerSchedule>();
        services.AddHostedService<SamplePrayer>();
        services.AddPermissions(PrayerPermissions.All);
        services.AddNotificationKinds(PrayerNotifications.All);

        services.AddSystemRole(new SystemRole(PrayerPermissions.PastoralTeamRole, "Pastoral team",
            "Reads every prayer request, keeps notes on them, follows them up, and chooses what the prayer wall shows", ScopeKind.Global,
            ["prayer.*"], Feature: RoleFeatures.Prayer));
        services.AddSystemRole(new SystemRole(PrayerPermissions.WallRole, "Prayer wall",
            "Reads only the prayer requests that ask for the prayer wall, and chooses what the wall shows", ScopeKind.Global,
            [PrayerPermissions.WallNode], Feature: RoleFeatures.Prayer));

        services.AddFeed(new FeedDefinition(PrayerFeeds.Wall, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(PrayerFeeds.Mine, FeedAudience.OwnRecords));
        services.AddFeed(new FeedDefinition(PrayerFeeds.Inbox, FeedAudience.Holding(PrayerPermissions.Wall)));
        services.AddFeed(new FeedDefinition(PrayerFeeds.Approving, FeedAudience.Holding(PrayerPermissions.Wall)));
        services.AddFeed(new FeedDefinition(PrayerFeeds.Care, FeedAudience.Holding(PrayerPermissions.Read)));
        services.AddDestinations(PrayerDestinations.All);
        services.AddSurfaces(PrayerTiles.Surfaces);
        return services;
    }

    public static IEndpointRouteBuilder MapPrayerApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<PrayerService>();
        endpoints.MapGrpcService<PastoralService>();
        return endpoints;
    }
}
