using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.Destinations;

public sealed class OpenedPlaceView(DestinationKind kind, [DynamicallyAccessedMembers(OpenedPlaceView.ComponentMembers)] Type component)
{
    public const string PlaceParameter = "Place";

    private const DynamicallyAccessedMemberTypes ComponentMembers = DynamicallyAccessedMemberTypes.All;

    public DestinationKind Kind { get; } = kind;

    [DynamicallyAccessedMembers(ComponentMembers)]
    public Type Component { get; } = component;
}

public static class OpenedPlaceServiceCollectionExtensions
{
    public static IServiceCollection AddOpenedPlaceView<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(this IServiceCollection services, DestinationKind kind) =>
        services.AddSingleton(new OpenedPlaceView(kind, typeof(TComponent)));
}
