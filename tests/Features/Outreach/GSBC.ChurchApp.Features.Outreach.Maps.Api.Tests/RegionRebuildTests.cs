using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

[Integration("A region's files are content-addressed in the bucket and its feed record and version change only when a rebuild changed them; files older than the previous version are deleted")]
public sealed class RegionRebuildTests : IAsyncLifetime
{
    private MapsHost _host = null!;
    private string _area = "";

    public async ValueTask InitializeAsync()
    {
        _host = await MapsHost.StartAsync();
        _area = (await _host.WithServiceAsync(maps => maps.AddAsync(new AddMapArea
        {
            Name = "Test area", CentreLatitude = Streets.Origin.Lat, CentreLongitude = Streets.Origin.Lon, RadiusKm = 1.5,
        }))).Id;
    }

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task ARebuildKeepsTheVersionUntilTheStreetsChangeAndOldFilesGo()
    {
        RegionRecord first = await BuildAsync();
        Assert.Equal(1, first.Version);
        Assert.NotNull(await _host.FindAsync(first.StreetsKey));
        Assert.NotNull(await _host.FindAsync(first.MapKey));
        OutreachRegion published = await PublishedAsync(first.Id);
        Assert.EndsWith(first.StreetsKey, published.Streets.Url);
        Assert.Equal(2, published.Letterboxes);

        Assert.Equal(1, (await RebuildAsync()).Version);
        Assert.Equal(1, await _host.FeedRowsAsync(first.Id));

        _host.Sources.AddHome(new Flat(220, 15));
        RegionRecord second = await RebuildAsync();
        _host.Sources.AddHome(new Flat(240, -15));
        RegionRecord third = await RebuildAsync();

        Assert.Equal((2, 3), (second.Version, third.Version));
        Assert.Equal(4, (await PublishedAsync(first.Id)).Letterboxes);
        Assert.Null(await _host.FindAsync(first.StreetsKey));
        Assert.NotNull(await _host.FindAsync(second.StreetsKey));
        Assert.NotNull(await _host.FindAsync(third.StreetsKey));
        Assert.Equal(first.MapKey, third.MapKey);
        Assert.NotNull(await _host.FindAsync(third.MapKey));
    }

    private async Task<RegionRecord> RebuildAsync()
    {
        await _host.WithServiceAsync(maps => maps.RebuildAsync(new MapAreaRef { Id = _area }));
        return await BuildAsync();
    }

    private async Task<RegionRecord> BuildAsync()
    {
        await _host.BuildAsync(_area);
        return Assert.Single(await _host.RegionsAsync(_area));
    }

    private async Task<OutreachRegion> PublishedAsync(string regionId)
    {
        FeedChangeRecord row = (await _host.LatestFeedRowAsync(regionId))!;
        return OutreachMapsContractsModel.Instance.Deserialize<OutreachRegion>(new MemoryStream(row.Payload));
    }
}
