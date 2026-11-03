using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSBC.ChurchApp.Platform.Client.Destinations;

public static class DestinationServiceCollectionExtensions
{
    public static IServiceCollection AddDestinations(this IServiceCollection services, DestinationSet destinations)
    {
        services.TryAddSingleton<DestinationCatalog>();
        services.TryAddSingleton(provider => new DestinationsClient(provider.GetRequiredService<Grpc.Core.ChannelBase>(), provider.GetRequiredService<DestinationCatalog>()));
        if (services.Any(service => ReferenceEquals(service.ImplementationInstance, destinations)))
            return services;
        return services.AddSingleton(destinations);
    }

    public static IServiceCollection AddPlaceAction(this IServiceCollection services, PlaceAction action) =>
        services.AddSingleton(action);

    public static IServiceCollection AddPlacePreview<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        this IServiceCollection services, Func<Destination, bool> shows) =>
        services.AddSingleton(new PlacePreview(shows, typeof(TComponent)));
}
