using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.News.Contracts;

public static class NewsDestinations
{
    public static readonly DestinationPage News = new("/news", "News", AppIcons.News, DestinationAreas.Pages, "News", Audience.Everyone, Rank: 15);

    public static readonly DestinationArea Area = new("news", "News", "news", 15);

    public static readonly DestinationKind Post = new("news.post", Area, "Church news post", "news", "news");

    public static string PostPath(string id) => Post.PathOf(id);

    public static readonly DestinationSet All = new([News], [Post]);
}
