using GSBC.ChurchApp.Features.Ministries.Client;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Ministries.Admin;

public static class MinistriesAdminExtensions
{
    public static IServiceCollection AddMinistriesAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Ministries", typeof(MinistriesAdminExtensions).Assembly,
            [new NavEntry("Ministries", "ministries", "users", MinistriesPermissions.People, Area: NavAreas.People,
                Summary: "Each ministry's people, news, calendars, files and the ministries under it, for its leaders and for staff"),
             new NavEntry("Life Groups", "life-groups", "book-open", MinistriesPermissions.Settings, Area: NavAreas.People,
                Summary: "The Sunday classes: what's on now, what's next, and the all-year groups")],
            [MinistriesFeeds.Ministries], []));
        return services
            .AddMinistriesClient()
            .AddSingleton<OnlineMinistries>()
            .AddSingleton<IMinistryReads>(provider => provider.GetRequiredService<OnlineMinistries>())
            .AddSingleton<IMinistryWrites>(provider => provider.GetRequiredService<OnlineMinistries>())
            .AddSingleton<ILinkableCalendars>(provider => provider.GetRequiredService<OnlineMinistries>())
            .AddSingleton(MinistryLinks.Admin);
    }
}
