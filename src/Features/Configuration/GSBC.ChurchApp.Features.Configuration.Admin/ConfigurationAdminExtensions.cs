using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Features.Configuration.Client.Components;
using GSBC.ChurchApp.Features.Configuration.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Scopes;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Configuration.Admin;

public static class ConfigurationAdminExtensions
{
    public static IServiceCollection AddConfigurationAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Configuration", typeof(ConfigurationAdminExtensions).Assembly,
            [
                new NavEntry("App styling", "app-styling", "pencil", HubSurfaces.Edit, Area: NavAreas.TheApp, Summary: "Change the app's home screen, colours and fonts, then publish"),
            ]));
        return services.AddConfigurationClient().AddSingleton<IOfficeWeekSource, DraftsOfficeWeek>().AddSingleton<IScopeNamer, DraftScopeNames>()
            .AddPlacePreview<HomePlacePreview>(place => place.Path == ShellDestinations.Home.Path)
            .AddPlaceAction(new PlaceAction("Change it in a draft", "pencil", place => place.Path == ShellDestinations.Home.Path ? $"app-styling?area={HubSurfaces.Area.Key}" : null, HubSurfaces.Edit, Leads: true));
    }
}
