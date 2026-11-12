using GSBC.ChurchApp.Features.Rosters.Client.Components;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Rosters.Client;

public static class RostersClientExtensions
{
    public static readonly IReadOnlyList<string> PrivateFeeds = [RostersFeeds.Ministries, RostersFeeds.Away];

    private const int AfterFamily = 30;

    public static IServiceCollection AddRostersOnPhone(this IServiceCollection services) =>
        services
            .AddRostersClient()
            .AddSingleton<IRosterReads, StoredRosters>()
            .AddSingleton(new AccountSection("rosters-away", AfterFamily, builder =>
            {
                builder.OpenComponent<YourDaysAway>(0);
                builder.CloseComponent();
            }));

    public static IServiceCollection AddRostersClient(this IServiceCollection services) =>
        services
            .AddSingleton<RosterWrites>()
            .AddSingleton(new MinistryPartView(RosterParts.Key, On))
            .AddHubTile<RosterTile>(RostersTiles.Roster);

    private static RenderFragment On(MinistryOnPage on) => builder =>
    {
        builder.OpenComponent<RosterArea>(0);
        builder.AddComponentParameter(1, nameof(RosterArea.On), on);
        builder.CloseComponent();
    };
}
