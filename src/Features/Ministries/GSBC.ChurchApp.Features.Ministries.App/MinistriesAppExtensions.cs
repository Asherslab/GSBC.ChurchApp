using GSBC.ChurchApp.Features.Ministries.Client;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Ministries.App;

public static class MinistriesAppExtensions
{
    public static IServiceCollection AddMinistriesApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Ministries", typeof(MinistriesAppExtensions).Assembly,
            [new NavEntry("Ministries", "ministries", "users"), new NavEntry("Life Groups", "life-groups", "book-open")],
            MinistriesFeeds.OnPhones, MinistriesFeeds.PrivateOnPhones));
        return services.AddMinistriesOnPhone().AddSingleton(MinistryLinks.App);
    }
}
