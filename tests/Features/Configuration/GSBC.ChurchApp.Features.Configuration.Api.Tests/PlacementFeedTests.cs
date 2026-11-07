using System.Security.Claims;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

[Integration("Publishing writes each placement and the event layer to the placements feed, and taking one away writes its tombstone")]
public sealed class PlacementFeedTests : ConfigurationHost
{
    [Fact]
    public async Task PublishingReachesPhonesThroughTheFeedAndARemovalDeletesThere()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        Placement saved = Assert.Single(await PublishNowAsync(staff, HubSurfaces.Area, null, LinkTile("Bible")));

        await PublishNowAsync(staff, HubSurfaces.Area, null, Remove(saved));

        List<FeedChangeRecord> feed = await FeedAsync(ConfigurationFeeds.Placements);
        Assert.Equal([false, true], feed.Select(change => change.Deleted));
        Assert.All(feed, change => Assert.Equal(saved.Id, change.RecordId));
        Placement sent = ConfigurationContractsModel.Instance.Deserialize<Placement>(new MemoryStream(feed[0].Payload));
        Assert.Equal("Bible", ValueOf(sent, HubSurfaces.LinkTile.Title.Key));
        Assert.Equal(ConfigurationFeeds.GroupOf(HubSurfaces.Area.Key, null), feed[0].GroupKey);
    }

    [Fact]
    public async Task TheEventLayerReachesPhonesAsOneFeedRecordAndTurningItOffDeletesIt()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        Placement map = Assert.Single(await PublishNowAsync(staff, HubSurfaces.Area, null, LinkTile("Site map")));
        Draft conference = await NewDraftAsync(staff, "Conference 2027");
        await SaveAsync(staff, conference, Change(map, (HubSurfaces.LinkTile.Title.Key, "Conference site map")));
        DateTime starts = Clock.UtcNow.AddDays(10);

        await Drafts(editor => editor.TurnOnAsync(staff, new EventLayerRequest { DraftId = conference.Id, StartsAt = starts, EndsAt = starts.AddDays(3) }, Cancel));
        EventLayerMessage sent = ConfigurationContractsModel.Instance.Deserialize<EventLayerMessage>(new MemoryStream(LayerRecords(await FeedAsync(ConfigurationFeeds.Placements))[^1].Payload));
        Assert.Equal((conference.Id, starts), (sent.DraftId, sent.StartsAt!.Value), new LayerComparer());

        await Drafts(editor => editor.TurnOffAsync(staff, new DraftQuery { DraftId = conference.Id }, Cancel));

        Assert.True(LayerRecords(await FeedAsync(ConfigurationFeeds.Placements))[^1].Deleted);
    }

    private static List<FeedChangeRecord> LayerRecords(List<FeedChangeRecord> feed) =>
        feed.Where(change => change.RecordId == ConfigurationFeeds.EventLayerRecord).ToList();

    private sealed class LayerComparer : IEqualityComparer<(string, DateTime)>
    {
        public bool Equals((string, DateTime) left, (string, DateTime) right) =>
            left.Item1 == right.Item1 && Math.Abs((left.Item2 - right.Item2).TotalMilliseconds) < 1;

        public int GetHashCode((string, DateTime) value) => value.Item1.GetHashCode();
    }
}
