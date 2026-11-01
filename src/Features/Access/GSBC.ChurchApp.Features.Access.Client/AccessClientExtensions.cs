using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Access.Client;

public static class AccessClientExtensions
{
    public static IServiceCollection AddAccessClient(this IServiceCollection services) =>
        services.AddSingleton<AccessClient>().AddSingleton<IClientStartupTask, AccessLoader>();
}
