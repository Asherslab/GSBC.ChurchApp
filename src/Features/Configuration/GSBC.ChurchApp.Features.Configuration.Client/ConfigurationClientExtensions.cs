using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Configuration;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Features.Configuration.Client.Components.Tiles;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSBC.ChurchApp.Features.Configuration.Client;

public static class ConfigurationClientExtensions
{
    public static IServiceCollection AddConfigurationClient(this IServiceCollection services)
    {
        services.AddDestinations(ConfigurationDestinations.All).AddDestinations(ComingSoonDestinations.All)
            .AddSingleton<ConfigurationClient>().AddSingleton<DraftsClient>().AddSingleton<DatedChanges>()
            .AddSurfaces(HubSurfaces.All).AddSurfaces(AppearanceSurfaces.All)
            .AddHubTile<SermonTile>(WaitingTiles.Sermon)
            .AddHubTile<BulletinTile>(WaitingTiles.Bulletin)
            .AddHubTile<WeekTile>(WaitingTiles.Week);
        foreach (HubTileKind kind in WaitingTiles.All.Except([WaitingTiles.Sermon, WaitingTiles.Bulletin, WaitingTiles.Week]))
            services.AddHubTile<WordsTileView>(kind);
        return services;
    }

    public static IServiceCollection AddLocalSurfaceLayout(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        return services
            .AddSingleton<IPreviewMode, DraftPreview>()
            .AddSingleton<ISurfaceLayout, LocalSurfaceLayout>()
            .AddMediaPrefetch(provider => PlacementMedia.Prefetch(provider.GetRequiredService<SurfaceCatalog>()));
    }
}
