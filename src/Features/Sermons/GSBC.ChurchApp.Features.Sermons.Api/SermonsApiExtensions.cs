using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api;

public static class SermonsApiExtensions
{
    public static IServiceCollection AddSermonsApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSermonsData();
        services.AddBlobStore(configuration);
        services.Configure<WebsiteOptions>(configuration.GetSection(WebsiteOptions.Section));
        services.AddHttpClient<WebsiteClient>(client => client.Timeout = TimeSpan.FromMinutes(2));
        services.AddScoped<Artwork>();
        services.AddMediaSystemArea(Artwork.Area);
        services.AddMediaSystemArea(AudioCopier.Area);
        services.AddScoped<Speakers>();
        services.AddScoped<ServiceNames>();
        services.AddScoped<SermonPictures>();
        services.AddScoped<IMediaHolder>(provider => provider.GetRequiredService<SermonPictures>());
        services.AddScoped<SermonPublisher>();
        services.AddScoped<WebsiteReader>();
        services.AddScoped<SourceIdMove>();
        services.AddScoped<SermonUploads>();
        services.AddScoped<SermonDeletion>();
        services.AddScoped<IPersonData, SermonsPersonData>();
        services.AddScoped<SermonFollowing>();
        services.AddScoped<SermonAnnouncer>();
        services.AddNotificationKinds(SermonsNotifications.All);
        services.AddSystemRole(new SystemRole(SermonsPermissions.FollowerRole, "Sermon follower",
            "Hears when a new sermon is out", ScopeKind.Global, [SermonsPermissions.HearNewNode], PhoneHoldable: true, Hidden: true));
        services.AddSystemRole(new SystemRole(SermonsPermissions.SeriesFollowerRole, "Series follower",
            "Hears when a series has a new sermon", ScopeKind.SermonSeries, [SermonsPermissions.HearSeriesNode], PhoneHoldable: true, Hidden: true));
        services.AddScoped<PodcastFeedWriter>();
        services.Configure<PodcastOptions>(configuration.GetSection(PodcastOptions.Section));
        services.AddSingleton<AudioCopier>();
        services.AddHostedService(provider => provider.GetRequiredService<AudioCopier>());
        services.AddSingleton<WebsiteSchedule>();
        services.AddHostedService(provider => provider.GetRequiredService<WebsiteSchedule>());

        services.AddPermissions(SermonsPermissions.All);
        services.AddDestinations(SermonsDestinations.All);
        services.AddSurfaces(SermonsTiles.Surfaces);
        services.AddDestinationSource<SermonDestinationSource>();
        services.AddDestinationSource<SeriesDestinationSource>();
        services.AddDestinationSource<SpeakerDestinationSource>();
        services.AddSystemRole(new SystemRole(SermonsPermissions.EditorRole, "Sermons editor",
            "Corrects sermons, looks after series and speakers, and uploads sermons", ScopeKind.Global,
            [SermonsPermissions.EditNode, SermonsPermissions.UploadNode]));
        services.AddSystemRole(new SystemRole(SermonsPermissions.UploaderRole, "Sermon uploader",
            "Uploads Sunday's sermons (the AV team)", ScopeKind.Global, [SermonsPermissions.UploadNode]));
        services.AddFeed(new FeedDefinition(SermonsFeeds.Catalogue, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(SermonsFeeds.Series, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(SermonsFeeds.Speakers, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(SermonsFeeds.Progress, FeedAudience.OwnRecords));
        services.AddFeed(new FeedDefinition(SermonsFeeds.Admin, FeedAudience.Holding(SermonsPermissions.Edit)));
        services.AddFeed(new FeedDefinition(SermonsFeeds.Copying, FeedAudience.Holding(SermonsPermissions.Edit)));
        return services;
    }

    public static IEndpointRouteBuilder MapSermonsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<SermonsService>();
        endpoints.MapGrpcService<SermonLibraryService>();
        return endpoints;
    }
}
