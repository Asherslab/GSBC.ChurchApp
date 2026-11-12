using GSBC.ChurchApp.Features.Ministries.Client;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Ministries.App;

public static class MinistriesAppExtensions
{
    public static IServiceCollection AddMinistriesApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Ministries", typeof(MinistriesAppExtensions).Assembly,
            [new NavEntry("Ministries", "ministries", AppIcons.Ministries), new NavEntry("Life Groups", "life-groups", AppIcons.LifeGroups)],
            MinistriesFeeds.OnPhones, MinistriesFeeds.PrivateOnPhones));
        return services.AddMinistriesOnPhone().AddSingleton(MinistryLinks.App).AddSingleton<ILinkableCalendars, StoredLinkableCalendars>();
    }
}
