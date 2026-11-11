using GSBC.ChurchApp.Features.Prayer.Client.Components;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.Client;

public static class PrayerClientExtensions
{
    public static IServiceCollection AddPrayerOnPhone(this IServiceCollection services) =>
        services.AddPrayerClient().AddSingleton<PrayerOnPhone>().AddSingleton<PrayerApprovals>();

    public static IServiceCollection AddPrayerClient(this IServiceCollection services) =>
        services
            .AddDestinations(PrayerDestinations.All)
            .AddHubTile<PrayerTile>(PrayerTiles.Prayer);
}
