using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;

namespace GSBC.ChurchApp.Features.News.Client;

public sealed class StoredNews(ILocalFeeds feeds, IAccess access) : INewsReads
{
    private const int Page = 200;

    public IReadOnlyList<string> Feeds => [NewsFeeds.Posts, NewsFeeds.PrivatePosts, NewsFeeds.Desk];

    public async Task<IReadOnlyList<NewsPost>> PublishedAsync()
    {
        if (!feeds.IsAvailable)
            return [];
        List<NewsPost> posts = await ReadAllAsync(NewsFeeds.Posts);
        if (access.SignedIn)
            posts.AddRange(await ReadAllAsync(NewsFeeds.PrivatePosts));
        return posts.DistinctBy(post => post.Id).ToList();
    }

    public async Task<IReadOnlyList<NewsPost>> DeskAsync() =>
        feeds.IsAvailable && access.Can(NewsPermissions.Write) ? await ReadAllAsync(NewsFeeds.Desk) : [];

    private async Task<List<NewsPost>> ReadAllAsync(string feed)
    {
        List<NewsPost> posts = [];
        for (int offset = 0; ; offset += Page)
        {
            IReadOnlyList<StoredRecord> page = await feeds.ReadPageAsync(new FeedPageQuery(feed, Descending: true, Limit: Page, Offset: offset));
            posts.AddRange(page.Select(row => Decode(row.Payload)));
            if (page.Count < Page)
                return posts;
        }
    }

    private static NewsPost Decode(byte[] payload)
    {
        using MemoryStream stream = new(payload);
        return NewsContractsModel.Instance.Deserialize<NewsPost>(stream);
    }
}
