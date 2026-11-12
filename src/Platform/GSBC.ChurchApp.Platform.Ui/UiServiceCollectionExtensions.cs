using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Ui.Destinations;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSBC.ChurchApp.Platform.Ui;

public static class UiServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformUi(this IServiceCollection services, bool desktop = false)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddDestinations(ShellDestinations.All);
        return services.AddSingleton(new UiHost(desktop)).AddSingleton<Shell>().AddSingleton<UnsavedChanges>().AddSingleton<SidebarPins>().AddSingleton<Appearance>().AddSingleton<LinkOpener>().AddSingleton<SurfaceViews>()
            .AddSurfaceView<LinkTileView>(HubSurfaces.LinkTile.Surface)
            .AddHubTile<WordsTileView>(HubSurfaces.PlainTile);
    }
}
