using GSBC.ChurchApp.Features.News.Client;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.News.App;

public static class NewsAppExtensions
{
    public static IServiceCollection AddNewsApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("News", typeof(NewsAppExtensions).Assembly,
            [new NavEntry("News", "news", AppIcons.News)],
            [NewsFeeds.Posts], [NewsFeeds.PrivatePosts, NewsFeeds.Desk]));
        return services.AddNewsOnPhone();
    }
}
