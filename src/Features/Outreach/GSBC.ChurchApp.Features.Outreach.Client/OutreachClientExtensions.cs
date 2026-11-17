using GSBC.ChurchApp.Features.Outreach.Client.Components;
using GSBC.ChurchApp.Features.Outreach.Client.Phone;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Ui.Locations;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Client;

public static class OutreachClientExtensions
{
    public static IServiceCollection AddOutreachOnPhone(this IServiceCollection services) =>
        services.AddOutreachClient()
            .AddSingleton<OutreachStore>()
            .AddSingleton<RegionMaps>()
            .AddSingleton<WalkSession>()
            .AddSingleton<WalkActions>()
            .AddSingleton<WalkCompanion>()
            .AddSingleton<IClientStartupTask>(provider => provider.GetRequiredService<WalkCompanion>())
            .AddSingleton<WhereYouAre>()
            .AddSingleton<WalkChoices>()
            .AddSingleton<OutreachNearby>()
            .AddSingleton<WalkPlans>()
            .AddSingleton<WalkScreen>()
            .AddSingleton<OutreachReports>()
            .AddSingleton<OutreachClaims>()
            .AddSingleton<OutreachDayRoutes>()
            .AddSingleton<OutreachDayBoard>()
            .AddSingleton<MyOutreach>()
            .AddSingleton<OfflineMaps>()
            .AddSingleton<OutreachLocations>()
            .AddSingleton<ILocationSource>(provider => provider.GetRequiredService<OutreachLocations>())
            .AddSingleton<IKeptMapCover>(provider => provider.GetRequiredService<OutreachLocations>());

    public static IServiceCollection AddOutreachClient(this IServiceCollection services) =>
        services
            .AddDestinations(OutreachDestinations.All)
            .AddHubTile<OutreachTile>(OutreachTiles.Outreach);
}
