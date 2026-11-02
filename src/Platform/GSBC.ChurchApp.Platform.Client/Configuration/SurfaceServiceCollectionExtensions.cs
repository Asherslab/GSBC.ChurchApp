using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSBC.ChurchApp.Platform.Client.Configuration;

public static class SurfaceServiceCollectionExtensions
{
    public static IServiceCollection AddSurfaces(this IServiceCollection services, SurfaceSet surfaces)
    {
        services.TryAddSingleton<SurfaceCatalog>();
        return services.AddSingleton(surfaces);
    }
}
