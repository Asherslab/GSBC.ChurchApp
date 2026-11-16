using System.Security.Claims;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
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

        List<Placement> after = await LiveAsync(staff, HubSurfaces.Area);
        Assert.Equal(before.Select(tile => (tile.Id, tile.Order, tile.Audience)), after.Select(tile => (tile.Id, tile.Order, tile.Audience)));
        Assert.Equal([HubSurfaces.LinkTile.Surface.Key, TestSurfaces.WhatsNext.Surface.Key, TestSurfaces.WhatsNext.Surface.Key], after.Select(tile => tile.Surface));
        Assert.Equal(["columns=2", "rows=2"],
            after.Where(tile => tile.Surface == TestSurfaces.WhatsNext.Surface.Key).Select(tile => string.Join(",", tile.Values.Select(value => $"{value.Key}={value.Value}"))).Order());
        Assert.Contains(await FeedAsync(ConfigurationFeeds.Placements), change => change.RecordId == after[1].Id && change.Version == after[1].Revision);
    }

    private Task<int> ReplaceAsync() =>
        InScopeAsync(services => TileReplacing.ReplaceOnceAsync(services.GetRequiredService<ConfigurationDbContext>(), services.GetRequiredService<PlacementEditor>(),
            TestSurfaces.WhatsNext.Surface, Cancel));
}
