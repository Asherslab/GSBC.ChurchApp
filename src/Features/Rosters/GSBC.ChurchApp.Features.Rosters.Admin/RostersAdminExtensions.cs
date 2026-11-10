using GSBC.ChurchApp.Features.Rosters.Client;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Rosters.Admin;

public static class RostersAdminExtensions
{
    public static IServiceCollection AddRostersAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Rosters", typeof(RostersAdminExtensions).Assembly, [], [RostersFeeds.Changes], []));
        return services
            .AddRostersClient()
            .AddSingleton<IRosterReads, OnlineRosters>();
    }
}
