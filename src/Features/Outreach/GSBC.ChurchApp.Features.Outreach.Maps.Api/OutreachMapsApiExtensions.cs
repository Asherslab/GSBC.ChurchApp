using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Places;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Sync;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api;

public static class OutreachMapsApiExtensions
{
    private const string UserAgent = "GSBC-ChurchApp/1.0 (+https://github.com/Asherslab/GSBC.ChurchApp)";

    public static IServiceCollection AddOutreachMapsApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOutreachMapsData();
        services.Configure<MapSourcesOptions>(configuration.GetSection(MapSourcesOptions.Section));
        services.AddHttpClient<IMapSources, MapSources>(client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });
        services.AddMemoryCache();
        services.AddScoped<AreaBuilder>();
        services.AddScoped<MapSquares>();
        services.AddScoped<PlaceIndex>();
        services.AddScoped<AddressIndex>();
        services.AddScoped<IAddressSearch>(provider => provider.GetRequiredService<AddressIndex>());
        services.AddScoped<RegionEstimates>();
        services.AddScoped<OverviewMap>();
        services.AddSingleton<AreaBuildRunner>();
        services.AddHostedService(provider => provider.GetRequiredService<AreaBuildRunner>());
        services.AddPermissions(OutreachMapsPermissions.All);
        services.AddFeed(new FeedDefinition(OutreachMapsFeeds.Regions, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(OutreachMapsFeeds.Builds, FeedAudience.Holding(OutreachMapsPermissions.Manage)));
        return services;
    }

    public static IEndpointRouteBuilder MapOutreachMapsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<OutreachMapsService>();
        return endpoints;
    }
}
