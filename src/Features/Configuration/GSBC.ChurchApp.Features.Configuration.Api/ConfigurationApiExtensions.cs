using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Configuration.Api;

public static class ConfigurationApiExtensions
{
    public static IServiceCollection AddConfigurationApi(this IServiceCollection services)
    {
        services.AddConfigurationData();
        services.AddScoped<PlacementEditor>();
        services.AddPermissions(HubSurfaces.Edit, AppearanceSurfaces.Edit);
        services.AddSurfaces(HubSurfaces.All).AddSurfaces(AppearanceSurfaces.All);
        services.AddFeed(new FeedDefinition(ConfigurationFeeds.Placements, FeedAudience.Public));
        return services;
    }

    public static IEndpointRouteBuilder MapConfigurationApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<ConfigurationService>();
        return endpoints;
    }
}
