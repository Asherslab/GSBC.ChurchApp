using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Client.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Ui.Surfaces;

public sealed record SurfaceViewRegistration(Surface Surface, Func<ArrangedSurface, RenderFragment> View);

public static class HubTileViews
{
    public static IServiceCollection AddHubTile<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TView>(this IServiceCollection services, HubTileKind kind)
        where TView : IComponent =>
        services.AddSurfaces(new SurfaceSet([kind.Surface])).AddSingleton(kind).AddSurfaceView<TView>(kind.Surface);

    public static IServiceCollection AddSurfaceView<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TView>(this IServiceCollection services, Surface surface)
        where TView : IComponent =>
        services.AddSingleton(new SurfaceViewRegistration(surface, entry => builder =>
        {
            builder.OpenComponent<TView>(0);
            builder.AddComponentParameter(1, "Entry", entry);
            builder.CloseComponent();
        }));
}
