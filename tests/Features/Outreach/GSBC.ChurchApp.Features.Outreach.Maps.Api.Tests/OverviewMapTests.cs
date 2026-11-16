using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

[Integration("The Australia overview is one content-addressed file in the bucket, replaced (never piling up) when it's refreshed, and never in a feed phones sync")]
public sealed class OverviewMapTests : IAsyncLifetime
{
    private MapsHost _host = null!;

    public async ValueTask InitializeAsync() => _host = await MapsHost.StartAsync();

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task TheOverviewIsOneImmutableFileForTheAdminPanelAndNeverInAFeed()
    {
        Assert.Empty((await OverviewAsync()).Map.Url);
        Assert.True(await DueAsync());

        await RefreshAsync();

        MapOverview overview = await OverviewAsync();
        string key = KeyOf(overview);
        Assert.Equal(6, overview.MaxZoom);
        Assert.Equal(OverviewMap.Australia.North, overview.Bounds.North);
        Assert.Equal("application/vnd.pmtiles", (await _host.FindAsync(key))!.ContentType);
        Assert.False(await DueAsync());
        Assert.Equal(0, await _host.WithDbAsync(db => db.FeedChanges.CountAsync()));
        Assert.Equal(0, await _host.FeedRowsAsync(OverviewMap.Id));
    }

    [Fact]
    public async Task ARefreshReplacesTheFileAndDeletesTheOldOneAfterThirtyDays()
    {
        await RefreshAsync();
        string first = KeyOf(await OverviewAsync());

        _host.Clock.Advance(TimeSpan.FromDays(29));
        Assert.False(await DueAsync());
        _host.Clock.Advance(TimeSpan.FromDays(2));
        Assert.True(await DueAsync());

        _host.Sources.Tiles = TileArchives.World(5);
        await RefreshAsync();

        string second = KeyOf(await OverviewAsync());
        Assert.NotEqual(first, second);
        Assert.Null(await _host.FindAsync(first));
        Assert.NotNull(await _host.FindAsync(second));
        Assert.Equal(1, await _host.WithDbAsync(db => db.Overviews.CountAsync()));
    }

    private Task<MapOverview> OverviewAsync() => _host.WithServiceAsync(maps => maps.OverviewAsync(new MapOverviewRequest()));

    private Task<bool> DueAsync() => _host.WithOverviewAsync(overview => overview.DueAsync(TestContext.Current.CancellationToken));

    private Task RefreshAsync() => _host.WithOverviewAsync(async overview =>
    {
        await overview.RefreshAsync(TestContext.Current.CancellationToken);
        return true;
    });

    private static string KeyOf(MapOverview overview) => overview.Map.Url[(overview.Map.Url.LastIndexOf('/') + 1)..];
}
