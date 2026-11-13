using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Media;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.News.Client;

public static class NewsClientExtensions
{
    public static IServiceCollection AddNewsOnPhone(this IServiceCollection services) =>
        services.AddNewsClient().AddSingleton<INewsReads, StoredNews>()
            .AddMediaPrefetch(_ => NewsMedia.Prefetch(NewsFeeds.Posts))
            .AddMediaPrefetch(_ => NewsMedia.Prefetch(NewsFeeds.PrivatePosts));

    public static IServiceCollection AddNewsClient(this IServiceCollection services) =>
        services.AddDestinations(NewsDestinations.All).AddSingleton<NewsWrites>();
}
