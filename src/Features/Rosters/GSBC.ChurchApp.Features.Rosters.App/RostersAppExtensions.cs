using GSBC.ChurchApp.Features.Rosters.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Rosters.App;

public static class RostersAppExtensions
{
    public static IServiceCollection AddRostersApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Rosters", typeof(RostersAppExtensions).Assembly, [], PrivateFeeds: RostersClientExtensions.PrivateFeeds));
        return services.AddRostersOnPhone();
    }
}
