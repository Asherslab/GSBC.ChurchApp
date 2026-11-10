using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Online;
using GSBC.ChurchApp.Platform.Client.People;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Access.Client;

[AdminOnly]
public static class AccessClientExtensions
{
    public static IServiceCollection AddAccessClient(this IServiceCollection services) =>
        services.AddSingleton<AccessClient>().AddSingleton<IRoleDirectory, AccessRoleDirectory>().AddSingleton<IClientStartupTask, AccessLoader>()
            .AddSingleton<IPlacePeopleSource, AccessPlacePeopleSource>();
}
