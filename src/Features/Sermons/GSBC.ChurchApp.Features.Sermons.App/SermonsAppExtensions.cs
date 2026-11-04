using GSBC.ChurchApp.Features.Sermons.Client;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.App;

public static class SermonsAppExtensions
{
    public static IServiceCollection AddSermonsApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Sermons", typeof(SermonsAppExtensions).Assembly,
            [new NavEntry("Sermons", "sermons", "sermons", Placement: NavPlacement.Tab)],
            SermonCatalogue.Feeds,
            Listening.Feeds));
        return services.AddSermonsOnPhone();
    }
}
