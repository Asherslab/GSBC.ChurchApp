using GSBC.ChurchApp.Platform.Client.Configuration;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSBC.ChurchApp.Features.Configuration.Client;

public static class ConfigurationClientExtensions
{
    public static IServiceCollection AddConfigurationClient(this IServiceCollection services) =>
        services.AddSingleton<ConfigurationClient>().AddSingleton<DraftsClient>().AddSingleton<DatedChanges>().AddSurfaces(HubSurfaces.All).AddSurfaces(AppearanceSurfaces.All);

    public static IServiceCollection AddLocalSurfaceLayout(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        return services
            .AddSingleton<IPreviewMode, DraftPreview>()
            .AddSingleton<ISurfaceLayout, LocalSurfaceLayout>()
            .AddMediaPrefetch(provider => PlacementMedia.Prefetch(provider.GetRequiredService<SurfaceCatalog>()));
    }
}
