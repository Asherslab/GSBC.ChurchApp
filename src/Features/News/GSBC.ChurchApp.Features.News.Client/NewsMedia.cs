using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.News.Client;

internal static class NewsMedia
{
    private const int NewestPosts = 30;

    public static MediaPrefetch Prefetch(string feed) =>
        new(new FeedPageQuery(feed, Descending: true, Limit: NewestPosts), In);

    private static IEnumerable<MediaRef> In(StoredRecord record)
    {
        NewsPost post = NewsContractsModel.Instance.Deserialize<NewsPost>(new MemoryStream(record.Payload));
        return post.Body.Media.Where(media => media.IsImage || media.IsVideo || media.IsAudio);
    }
}
