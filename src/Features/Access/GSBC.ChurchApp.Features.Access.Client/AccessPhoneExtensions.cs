using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Access.Client;

public static class AccessPhoneExtensions
{
    public static IServiceCollection AddAccessOnPhone(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Access", typeof(AccessPhoneExtensions).Assembly, [], PrivateFeeds: [AccessFeeds.Grants]));
        return services.AddSingleton<IClientStartupTask, PhoneAccessLoader>();
    }
}
