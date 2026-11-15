using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public static class OutreachAdminExtensions
{
    public static IServiceCollection AddOutreachAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Outreach", typeof(OutreachAdminExtensions).Assembly,
            [new NavEntry("Outreach", "outreach", AppIcons.Outreach, OutreachPermissions.Reports, Area: NavAreas.MinistriesAndGroups,
                Summary: "Outreach campaigns and their settings: rest period, pace, bundles and what's kept")],
            [OutreachFeeds.Desk], []));
        return services.AddOutreachClient().AddSingleton<OutreachDeskClient>().AddSingleton<OutreachMapsClient>()
            .AddSingleton(_ => new StreetGraphLoader(new HttpClient()))
            .AddSingleton<Letterboxes>();
    }
}
