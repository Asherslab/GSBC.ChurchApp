using GSBC.ChurchApp.Features.Ministries.Client.Components;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Scopes;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class MinistriesClientExtensions
{
    public static readonly IReadOnlyList<string> Feeds = [MinistriesFeeds.Ministries];

    public static IServiceCollection AddMinistriesClient(this IServiceCollection services) =>
        services
            .AddDestinations(MinistriesDestinations.All)
            .AddSingleton<MinistriesClient>()
            .AddSingleton<IScopeNamer, MinistryScopeNames>()
            .AddHubTile<MinistriesTile>(MinistriesTiles.Ministries)
            .AddHubTile<LifeGroupsTile>(MinistriesTiles.LifeGroups);
}
