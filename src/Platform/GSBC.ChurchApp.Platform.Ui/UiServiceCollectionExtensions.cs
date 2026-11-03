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
    public static IServiceCollection AddPlatformUi(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddDestinations(ShellDestinations.All);
        return services.AddSingleton<Shell>().AddSingleton<Appearance>().AddSingleton<LinkOpener>().AddSingleton(new SurfaceViews()
            .Add(HubSurfaces.LinkTile.Surface, entry => builder =>
            {
                builder.OpenComponent<LinkTileView>(0);
                builder.AddComponentParameter(1, nameof(LinkTileView.Entry), entry);
                builder.CloseComponent();
            }));
    }
}
