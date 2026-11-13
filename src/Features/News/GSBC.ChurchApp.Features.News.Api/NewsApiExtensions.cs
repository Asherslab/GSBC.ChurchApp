using GSBC.ChurchApp.Features.News.Api.Data;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.News;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.News.Api;

public static class NewsApiExtensions
{
    public static IServiceCollection AddNewsApi(this IServiceCollection services)
    {
        services.AddNewsData();
        services.AddScoped<NewsPublisher>().AddScoped<NewsTelling>().AddScoped<NewsDesk>();
        services.AddScoped<IChurchNews, ChurchNewsShares>();
        services.AddScoped<IPersonData, NewsPersonData>();
        services.AddHostedService<NewsSchedule>();
        services.AddPermissions(NewsPermissions.All);
        services.AddNotificationKinds(NewsNotifications.All);
        services.AddMediaSystemArea(NewsMedia.Area);

        services.AddSystemRole(new SystemRole(NewsPermissions.WriterRole, "Church news Writer",
            "Write church news, try its notification on their own phone, and ask for it to be published", ScopeKind.Global,
            [NewsPermissions.Write.Node, NewsPermissions.AnnouncementsWriteNode], Feature: RoleFeatures.News, Kits: [RoleKits.Pictures],
            Replaces: [NewsPermissions.AnnouncementsWriterRole]));
        services.AddSystemRole(new SystemRole(NewsPermissions.PublisherRole, "Church news Publisher",
            "Write, publish, schedule and delete church news, send its notifications, and put ministries' posts into it", ScopeKind.Global,
            [NewsPermissions.Write.Node, NewsPermissions.Publish.Node, NewsPermissions.AnnouncementsWriteNode, NewsPermissions.AnnouncementsSendNode],
            Feature: RoleFeatures.News, Kits: [RoleKits.Pictures], Replaces: [NewsPermissions.AnnouncementsSenderRole, "announcer"]));

        services.AddFeed(new FeedDefinition(NewsFeeds.Posts, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(NewsFeeds.PrivatePosts, FeedAudience.EachRow));
        services.AddFeed(new FeedDefinition(NewsFeeds.Desk, FeedAudience.Holding(NewsPermissions.Write)));
        services.AddDestinations(NewsDestinations.All);
        services.AddSurfaces(NewsTiles.Surfaces);
        return services;
    }

    public static IEndpointRouteBuilder MapNewsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<NewsService>();
        return endpoints;
    }
}
