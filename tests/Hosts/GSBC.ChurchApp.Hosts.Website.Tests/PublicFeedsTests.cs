using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Hosts.Website.Feeds;
using GSBC.ChurchApp.Platform.Contracts.Sync;

namespace GSBC.ChurchApp.Hosts.Website.Tests;

public sealed class PublicFeedsTests
{
    [Fact]
    public void AnEventTheApiTakesAwayLeavesTheWebsite()
    {
        PublicFeeds feeds = new();
        feeds.Apply(Watched(Batch(EventsFeeds.Public, 2, Row("carols"), Row("breakfast"))));
        Assert.Equal(["breakfast", "carols"], feeds.Current.Events.Select(occurrence => occurrence.Id).Order());

        feeds.Apply(Watched(Batch(EventsFeeds.Public, 3, new FeedChange { Id = "carols", Deleted = true })));

        Assert.Equal(["breakfast"], feeds.Current.Events.Select(occurrence => occurrence.Id));
    }

    [Fact]
    public void AResetFeedForgetsEverythingItHeldBeforeItsNewRows()
    {
        PublicFeeds feeds = new();
        feeds.Apply(Watched(Batch(EventsFeeds.Public, 2, Row("carols"), Row("breakfast"))));

        FeedBatch reset = Batch(EventsFeeds.Public, 1, Row("camp"));
        reset.Reset = true;
        feeds.Apply(Watched(reset));

        Assert.Equal(["camp"], feeds.Current.Events.Select(occurrence => occurrence.Id));
        Assert.Contains(feeds.NextRequest().Cursors, cursor => cursor is { Feed: EventsFeeds.Public, Cursor: 1 });
    }

    [Fact]
    public void ItIsReadyOnlyOnceTheFirstReadHasNoMoreToCome()
    {
        PublicFeeds feeds = new();
        FeedBatch first = Batch(EventsFeeds.Public, 500, Row("carols"));
        first.HasMore = true;

        feeds.Apply(Watched(first));
        Assert.False(feeds.HasRead);

        feeds.Apply(Watched(Batch(EventsFeeds.Public, 501, Row("camp"))));
        Assert.True(feeds.HasRead);
    }

    private static WatchEvent Watched(params FeedBatch[] batches) => new() { Feeds = [.. batches], Origin = "origin" };

    private static FeedBatch Batch(string feed, long cursor, params FeedChange[] changes) => new() { Feed = feed, Cursor = cursor, Changes = [.. changes] };

    private static FeedChange Row(string id)
    {
        using MemoryStream payload = new();
        EventsContractsModel.Instance.Serialize(payload, new EventOccurrence { Id = id, Fields = new EventFields { Title = id } });
        return new FeedChange { Id = id, Payload = payload.ToArray() };
    }
}
