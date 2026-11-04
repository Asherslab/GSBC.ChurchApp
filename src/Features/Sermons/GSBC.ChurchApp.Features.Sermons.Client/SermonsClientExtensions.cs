using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public static class SermonsClientExtensions
{
    public static IServiceCollection AddSermonsOnPhone(this IServiceCollection services) =>
        services
            .AddSermonsClient()
            .AddSingleton<Listening>()
            .AddSingleton<IClientStartupTask>(provider => provider.GetRequiredService<Listening>())
            .AddSingleton<SermonPlayer>()
            .AddSingleton<SermonLibrary>();

    public static IServiceCollection AddSermonsClient(this IServiceCollection services) =>
        services
            .AddDestinations(SermonsDestinations.All)
            .AddSingleton<SermonCatalogue>()
            .AddHubTile<LatestSermonTile>(SermonsTiles.Latest);
}
