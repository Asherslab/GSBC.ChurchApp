using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Ministries.Client;

internal static class MinistryPostMedia
{
    private const int NewestPosts = 60;

    public static MediaPrefetch Prefetch(string feed) =>
        new(new FeedPageQuery(feed, Descending: true, Limit: NewestPosts), In);

    private static IEnumerable<MediaRef> In(StoredRecord record)
    {
        Post post = MinistriesContractsModel.Instance.Deserialize<Post>(new MemoryStream(record.Payload));
        return post.Content?.Media.Where(media => media.IsImage || media.IsVideo || media.IsAudio) ?? [];
    }
}
