using GSBC.ChurchApp.Features.Configuration.Client;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Configuration.App;

public static class ConfigurationAppExtensions
{
    public static IServiceCollection AddConfigurationApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Configuration", typeof(ConfigurationAppExtensions).Assembly,
            [
                new NavEntry("Previews", "previews", "eye"),
                new NavEntry("Drafts", "drafts", "pencil", HubSurfaces.Edit),
                new NavEntry("Arrange Home", $"configure/{HubSurfaces.Area.Key}", "layout", HubSurfaces.Edit),
                new NavEntry("Colours for everyone", $"configure/{AppearanceSurfaces.Area.Key}", "palette", AppearanceSurfaces.Edit),
            ],
            [ConfigurationFeeds.Placements, ConfigurationFeeds.Drafts]));
        return services.AddConfigurationClient().AddLocalSurfaceLayout();
    }
}
