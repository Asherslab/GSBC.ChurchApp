using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.News;

namespace GSBC.ChurchApp.Features.News.Client;

public sealed record NewsHome(IReadOnlyList<NewsItem> Church, IReadOnlyList<NewsItem> Yours, bool InMinistries, int Waiting, IReadOnlyList<NewsItem>? Every = null)
{
    public static async Task<NewsHome> LoadAsync(INewsReads news, IEnumerable<INewsSource> sources, IEveryMinistrysNews? every = null)
    {
        IReadOnlyList<NewsPost> published = await news.PublishedAsync();
        IReadOnlyList<NewsPost> desk = await news.DeskAsync();
        List<NewsFromSource> fromSources = [];
        foreach (INewsSource source in sources)
            fromSources.Add(await source.LoadAsync());
        return new NewsHome(
            published.Where(post => post.State == NewsState.Published).Select(NewsWords.Item).ToList(),
            fromSources.SelectMany(source => source.Items).ToList(),
            fromSources.Any(source => source.Applies),
            desk.Count(post => post.State is NewsState.WaitingToPublish or NewsState.Asked or NewsState.Draft),
            every is null ? null : (await every.LoadAsync()).Select(post => post.Item).ToList());
    }
}
