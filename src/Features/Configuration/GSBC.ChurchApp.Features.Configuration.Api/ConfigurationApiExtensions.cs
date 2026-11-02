using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;
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
        services.AddScoped<DraftEditor>();
        services.AddPermissions(HubSurfaces.Edit, HubSurfaces.Publish, AppearanceSurfaces.Edit, AppearanceSurfaces.Publish, ConfigurationPermissions.PreviewDraft);
        services.AddSystemRole(new SystemRole(ConfigurationPermissions.DraftReviewerRole, "Draft reviewer",
            "Sees a draft on their phone before it's published", ScopeKind.Draft, [ConfigurationPermissions.PreviewDraft.Node], AssignableBy: []));
        services.AddSystemRole(new SystemRole("app-editor", "App editor",
            "Arranges the app's Home screen and colours, sees every draft, and publishes", ScopeKind.Global, ["config.*"]));
        services.AddSurfaces(HubSurfaces.All).AddSurfaces(AppearanceSurfaces.All);
        services.AddFeed(new FeedDefinition(ConfigurationFeeds.Placements, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(ConfigurationFeeds.Drafts, FeedAudience.Holding(ConfigurationPermissions.PreviewDraft)));
        return services;
    }

    public static IEndpointRouteBuilder MapConfigurationApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<ConfigurationService>();
        endpoints.MapGrpcService<DraftsService>();
        return endpoints;
    }
}
