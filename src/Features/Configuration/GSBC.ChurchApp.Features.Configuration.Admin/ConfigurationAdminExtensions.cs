using GSBC.ChurchApp.Features.Configuration.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Configuration.Admin;

public static class ConfigurationAdminExtensions
{
    public static IServiceCollection AddConfigurationAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Configuration", typeof(ConfigurationAdminExtensions).Assembly,
            [new NavEntry("Home screen", $"configure/{HubSurfaces.Area.Key}", "layout", HubSurfaces.Edit)]));
        return services.AddConfigurationClient();
    }
}
