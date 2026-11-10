using GSBC.ChurchApp.Features.Prayer.Client;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.App;

public static class PrayerAppExtensions
{
    public static IServiceCollection AddPrayerApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Prayer", typeof(PrayerAppExtensions).Assembly,
            [new NavEntry("Prayer", "prayer", "prayer")],
            [PrayerFeeds.Wall], [PrayerFeeds.Mine]));
        return services.AddPrayerOnPhone();
    }
}
