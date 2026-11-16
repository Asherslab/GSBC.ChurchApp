using System.Security.Claims;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

[Integration("A retired tile kind's tiles on Home turn into the kind that replaces it once, where they were, with their size")]
public sealed class TileReplacingTests : ConfigurationHost
{
    [Fact]
    public async Task BulletinTilesBecomeWhatsNextOnceKeepingPlaceSizeAndWhoSeesThem()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        await PublishNowAsync(staff, HubSurfaces.Area, null,
            LinkTile("Give"),
            Add(WaitingTiles.Bulletin.Surface, HubSurfaces.Area, null, (HubTileKeys.Rows, "2"), (HubTileKeys.Title, "Read the bulletin"), (HubTileKeys.Layout, WaitingTiles.Layouts.Verse)),
            Add(WaitingTiles.Bulletin.Surface, HubSurfaces.Area, null, (HubTileKeys.Columns, "2")));
        List<Placement> before = await LiveAsync(staff, HubSurfaces.Area);

        Assert.Equal(2, await ReplaceAsync());
        Assert.Equal(0, await ReplaceAsync());

        Dictionary<string, Placement> after = (await LiveAsync(staff, HubSurfaces.Area)).ToDictionary(tile => tile.Id);
        Placement give = before.Single(tile => tile.Surface == HubSurfaces.LinkTile.Surface.Key);
        Assert.Equal(before.Select(tile => (tile.Id, tile.Order, tile.Audience)).Order(), after.Values.Select(tile => (tile.Id, tile.Order, tile.Audience)).Order());
        Assert.Equal(HubSurfaces.LinkTile.Surface.Key, after[give.Id].Surface);
        List<Placement> replaced = after.Values.Where(tile => tile.Id != give.Id).ToList();
        Assert.All(replaced, tile => Assert.Equal(TestSurfaces.WhatsNext.Surface.Key, tile.Surface));
        Assert.Equal(["columns=2", "rows=2"], replaced.Select(tile => string.Join(",", tile.Values.Select(value => $"{value.Key}={value.Value}"))).Order());
        List<FeedChangeRecord> feed = await FeedAsync(ConfigurationFeeds.Placements);
        Assert.All(replaced, tile => Assert.Contains(feed, change => change.RecordId == tile.Id && change.Version == tile.Revision));
    }

    private Task<int> ReplaceAsync() =>
        InScopeAsync(services => TileReplacing.ReplaceOnceAsync(services.GetRequiredService<ConfigurationDbContext>(), services.GetRequiredService<PlacementEditor>(),
            TestSurfaces.WhatsNext.Surface, Cancel));
}
