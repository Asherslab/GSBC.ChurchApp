using GSBC.ChurchApp.Features.News.Client;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.News.Admin;

public static class NewsAdminExtensions
{
    public static IServiceCollection AddNewsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("News", typeof(NewsAdminExtensions).Assembly,
            [new NavEntry("Church news", "news", AppIcons.News, NewsPermissions.Write, Area: NavAreas.ThisWeek,
                Summary: "News for the whole church: write, publish and schedule posts and their notifications, and answer ministries' asks")],
            [NewsFeeds.Desk], []));
        return services.AddNewsClient().AddSingleton<OnlineNews>().AddSingleton<INewsReads>(provider => provider.GetRequiredService<OnlineNews>())
            .AddPlaceAction(new PlaceAction("Tell the church about it", AppIcons.News, place => $"news?opens={Uri.EscapeDataString(place.Path)}", NewsPermissions.Write));
    }
}
