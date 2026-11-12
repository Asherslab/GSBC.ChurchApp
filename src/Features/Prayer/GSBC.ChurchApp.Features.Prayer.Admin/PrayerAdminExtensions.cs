using GSBC.ChurchApp.Features.Prayer.Client;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Notifications;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.Admin;

public static class PrayerAdminExtensions
{
    public static IServiceCollection AddPrayerAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Prayer", typeof(PrayerAdminExtensions).Assembly,
            [new NavEntry("Prayer", "prayer", AppIcons.Prayer, PrayerPermissions.Wall, Area: NavAreas.People,
                Summary: "Prayer requests from the app, for the pastoral team, and what the prayer wall shows")],
            [PrayerFeeds.Inbox], []));
        return services.AddPrayerClient().AddSingleton<PastoralClient>().AddSingleton<DigestSchedulesClient>();
    }
}
