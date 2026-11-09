using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.People;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Access.Client;

public static class AccessClientExtensions
{
    public static IServiceCollection AddAccessClient(this IServiceCollection services) =>
        services.AddSingleton<AccessClient>().AddSingleton<IRoleDirectory, AccessRoleDirectory>().AddSingleton<IClientStartupTask, AccessLoader>()
            .AddSingleton<IPlacePeopleSource, AccessPlacePeopleSource>();

    public static IServiceCollection AddAccessOnPhone(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Access", typeof(AccessClientExtensions).Assembly, [], PrivateFeeds: [AccessFeeds.Grants]));
        return services.AddSingleton<AccessClient>().AddSingleton<AccessLoader>().AddSingleton<IClientStartupTask, PhoneAccessLoader>();
    }
}
