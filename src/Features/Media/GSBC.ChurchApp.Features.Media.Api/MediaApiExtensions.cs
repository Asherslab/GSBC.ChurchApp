using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Media.Api;

public static class MediaApiExtensions
{
    public static IServiceCollection AddMediaApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediaData();
        services.AddBlobStore(configuration);
        services.AddScoped<MediaLibrary>();
        services.AddScoped<MediaSizer>();
        services.AddScoped<MediaHolders>();
        services.AddHostedService<MediaSizesBackfill>();
        services.AddScoped<MediaFolders>();
        services.AddScoped<IMediaSystemFolders>(provider => provider.GetRequiredService<MediaFolders>());
        services.AddScoped<IMediaReferences, MediaReferences>();
        services.AddDestinations(MediaDestinations.All);
        services.AddDestinationSource<MediaDestinationSource>();
        services.AddPermissions(MediaPermissions.All);
        services.AddFeed(new FeedDefinition(MediaFeeds.Library, FeedAudience.Holding(MediaPermissions.View)));
        services.AddSystemRole(new SystemRole("media-editor", "Media editor", "Upload images and files, keep them in folders, and see the media library",
            ScopeKind.Global, [MediaPermissions.ViewNode, MediaPermissions.UploadNode, MediaPermissions.DeleteNode, MediaPermissions.OrganiseNode]));
        services.AddHttpClient<GoogleFonts>(client => client.DefaultRequestHeaders.UserAgent.ParseAdd(GoogleFonts.BrowserAgent));
        services.AddScoped<FontsService>();
        return services;
    }

    public static IEndpointRouteBuilder MapMediaApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<MediaService>();
        endpoints.MapGrpcService<FontsService>();
        endpoints.MapGrpcService<MediaFoldersService>();
        return endpoints;
    }
}
