using GSBC.ChurchApp.Features.News.Api.Data;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Data;

namespace GSBC.ChurchApp.Features.News.Api;

internal sealed class NewsPublisher(NewsDbContext db)
{
    public void Changed(PostRecord post, DateTime now)
    {
        db.FeedChanges.Add(Row(NewsFeeds.Desk, post, now, NewsRules.View(post)));
        bool published = NewsRules.IsPublished(post);
        bool forEveryone = NewsRules.IsForEveryone(post);
        db.FeedChanges.Add(published && forEveryone ? Row(NewsFeeds.Posts, post, now, NewsRules.ForReaders(post)) : Gone(NewsFeeds.Posts, post.Id, now));
        db.FeedChanges.Add(published && !forEveryone
            ? Row(NewsFeeds.PrivatePosts, post, now, NewsRules.ForReaders(post), post.Audience)
            : Gone(NewsFeeds.PrivatePosts, post.Id, now));
    }

    public void Deleted(Guid id, DateTime now)
    {
        db.FeedChanges.Add(Gone(NewsFeeds.Desk, id, now));
        db.FeedChanges.Add(Gone(NewsFeeds.Posts, id, now));
        db.FeedChanges.Add(Gone(NewsFeeds.PrivatePosts, id, now));
    }

    private static FeedChangeRecord Row(string feed, PostRecord post, DateTime now, NewsPost payload, string? scope = null) => new()
    {
        Feed = feed,
        RecordId = post.Id.ToString(),
        ScopeId = scope,
        Version = now.Ticks,
        SortKey = (post.PublishedAt ?? post.CreatedAt).ToString("O"),
        Payload = NewsPayloads.Encode(payload),
    };

    private static FeedChangeRecord Gone(string feed, Guid id, DateTime now) =>
        new() { Feed = feed, RecordId = id.ToString(), Version = now.Ticks, Deleted = true };
}

internal static class NewsPayloads
{
    public static byte[] Encode<T>(T message)
    {
        using MemoryStream stream = new();
        NewsContractsModel.Instance.Serialize(stream, message);
        return stream.ToArray();
    }

    public static T Decode<T>(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        return NewsContractsModel.Instance.Deserialize<T>(stream);
    }
}
