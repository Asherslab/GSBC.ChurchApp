using GSBC.ChurchApp.Features.Prayer.Client.Components;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.Client;

public static class PrayerClientExtensions
{
    public static IServiceCollection AddPrayerOnPhone(this IServiceCollection services) =>
        services.AddPrayerClient().AddSingleton<PrayerOnPhone>();

    public static IServiceCollection AddPrayerClient(this IServiceCollection services) =>
        services
            .AddDestinations(PrayerDestinations.All)
            .AddHubTile<PrayerTile>(PrayerTiles.Prayer);

    public static IServiceCollection AddPastoralClient(this IServiceCollection services) =>
        services.AddPrayerClient().AddSingleton<PastoralClient>();
}
