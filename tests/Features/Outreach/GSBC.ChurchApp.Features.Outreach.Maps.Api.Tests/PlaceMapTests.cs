using GSBC.ChurchApp.Features.Outreach.Maps.Api.National;
using GSBC.ChurchApp.Platform.Contracts.Locations;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

[Integration("A place's map is one content-addressed file per block of the map, shared by places close together, and removed from the bucket once nothing uses it")]
public sealed class PlaceMapTests : IAsyncLifetime
{
    private MapsHost _host = null!;

    public async ValueTask InitializeAsync() => _host = await MapsHost.StartAsync();

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task PlacesCloseTogetherShareOneMapThatSurroundsThem()
    {
        LocationMap first = await PrepareAsync(-27.3501, 152.9690);
        LocationMap next = await PrepareAsync(-27.3503, 152.9692);

        Assert.Equal(first.Key, next.Key);
        Assert.True(first.Contains(-27.3501, 152.9690));
        Assert.InRange(first.North - first.South, 0.015, 0.025);
        Assert.Equal("application/vnd.pmtiles", (await _host.FindAsync(first.Key))!.ContentType);
        Assert.Equal(1, await _host.WithDbAsync(db => db.PlaceMaps.CountAsync()));
    }

    [Fact]
    public async Task NoMapIsMadeOutsideAustralia() => Assert.Empty((await PrepareAsync(51.5, -0.12)).Url);

    [Fact]
    public async Task AMapNothingUsesIsRemovedAfterTwoDaysAndAUsedOneStays()
    {
        LocationMap used = await PrepareAsync(-27.3501, 152.9690);
        LocationMap unused = await PrepareAsync(-28.1700, 153.5400);
        _host.LocationUses.Keys.Add(used.Key);

        Assert.Equal(0, await SweepAsync());
        _host.Clock.Advance(TimeSpan.FromDays(3));
        Assert.Equal(1, await SweepAsync());

        Assert.NotNull(await _host.FindAsync(used.Key));
        Assert.Null(await _host.FindAsync(unused.Key));
    }

    private Task<LocationMap> PrepareAsync(double latitude, double longitude) =>
        _host.WithScopeAsync(services => services.GetRequiredService<PlaceMaps>().PrepareAsync(latitude, longitude, TestContext.Current.CancellationToken));

    private Task<int> SweepAsync() =>
        _host.WithScopeAsync(services => services.GetRequiredService<PlaceMaps>().SweepAsync(TestContext.Current.CancellationToken));
}
