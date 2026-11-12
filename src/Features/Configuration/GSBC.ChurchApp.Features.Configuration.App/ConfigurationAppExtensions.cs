using GSBC.ChurchApp.Features.Configuration.Client;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Configuration.App;

public static class ConfigurationAppExtensions
{
    public static IServiceCollection AddConfigurationApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Configuration", typeof(ConfigurationAppExtensions).Assembly,
            [new NavEntry("Previews", "previews", AppIcons.Previews)],
            [ConfigurationFeeds.Placements],
            [ConfigurationFeeds.Drafts]));
        return services.AddConfigurationClient().AddLocalSurfaceLayout();
    }
}
