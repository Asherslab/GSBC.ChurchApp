using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.News.Client;

public static class NewsClientExtensions
{
    public static IServiceCollection AddNewsOnPhone(this IServiceCollection services) =>
        services.AddNewsClient().AddSingleton<INewsReads, StoredNews>();

    public static IServiceCollection AddNewsClient(this IServiceCollection services) =>
        services.AddDestinations(NewsDestinations.All).AddSingleton<NewsWrites>();
}
