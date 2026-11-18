using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

[Integration("Switching a region off deletes its files and feed record, switching it back on brings the same place back under the same id with a newer version, a region added on the edge is built alone, and a deleted area's ids are never given to another place")]
public sealed class RegionChangeTests : IAsyncLifetime
{
    private static readonly CellRef Home = new() { Row = 0, Column = 0 };
    private static readonly CellRef East = new() { Row = 0, Column = 1 };

    private MapsHost _host = null!;
    private MapArea _area = null!;

    public async ValueTask InitializeAsync()
    {
        _host = await MapsHost.StartAsync();
        _host.Sources.AddStreet("Heron Street", (3000, 0), (3300, 0));
        _host.Sources.AddHome("Heron Street", new Flat(3100, 15));
        _area = await AddAsync("Test area");
        await _host.BuildAsync(_area.Id);
    }

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task ARegionAddedOnTheEdgeIsBuiltAloneAndTheOthersAreLeftAsTheyWere()
    {
        RegionRecord home = Assert.Single(await _host.RegionsAsync(_area.Id));
        int homeRows = await _host.FeedRowsAsync(home.Id);
        _host.Sources.StreetsAskedFor.Clear();

        await ChangeAsync(on: [East]);
        await _host.BuildAsync(_area.Id);

        List<RegionRecord> regions = await _host.RegionsAsync(_area.Id);
        Assert.Equal(2, regions.Count);
        Assert.Equal(homeRows, await _host.FeedRowsAsync(home.Id));
        GeoBounds asked = Assert.Single(_host.Sources.StreetsAskedFor);
        Assert.False(asked.Contains(Streets.Point(-1400, 0)), "Only the new region's streets are read, not the whole area's");
        MapArea area = await AreaAsync();
        Assert.Equal(MapAreaStates.Built, area.State);
        Assert.All(area.Cells, cell => Assert.False(cell.Pending));
    }

    [Fact]
    public async Task SwitchingARegionOffDeletesItsFilesAndFeedRecordAndOnAgainBringsTheSamePlaceBack()
    {
        await ChangeAsync(on: [East]);
        await _host.BuildAsync(_area.Id);
        RegionRecord east = (await _host.RegionsAsync(_area.Id)).Single(region => region.Id != Assert.Single(_area.Cells).RegionId);

        await ChangeAsync(off: [East]);

        Assert.DoesNotContain(await _host.RegionsAsync(_area.Id), region => region.Id == east.Id);
        Assert.Null(await _host.FindAsync(east.StreetsKey));
        Assert.Null(await _host.FindAsync(east.MapKey));
        Assert.True((await _host.LatestFeedRowAsync(east.Id))!.Deleted);

        await ChangeAsync(on: [East]);
        await _host.BuildAsync(_area.Id);

        RegionRecord back = (await _host.RegionsAsync(_area.Id)).Single(region => region.Id == east.Id);
        Assert.Equal((east.CoreSouth, east.CoreWest, east.CoreNorth, east.CoreEast), (back.CoreSouth, back.CoreWest, back.CoreNorth, back.CoreEast));
        Assert.True(back.Version > east.Version, "Phones holding the old version download it again");
    }

    [Fact]
    public async Task ADeletedAreasRegionIdsAreNeverGivenToAnotherPlace()
    {
        string oldRegion = Assert.Single(_area.Cells).RegionId;
        await _host.WithServiceAsync(maps => maps.DeleteAsync(new MapAreaRef { Id = _area.Id }));

        MapArea again = await AddAsync("Test area", eastMetres: 40_000);

        Assert.NotEqual(_area.Id, again.Id);
        Assert.DoesNotContain(again.Cells, cell => cell.RegionId == oldRegion);
    }

    [Fact]
    public async Task SwitchingOffAskedFirstSaysWhatUsesTheRegionAndWhatItFrees()
    {
        string region = Assert.Single(_area.Cells).RegionId;
        await ChangeAsync(on: [East]);
        await _host.BuildAsync(_area.Id);
        _host.Uses.Uses.Add(new RegionUse { RegionId = region, Kind = RegionUseKinds.Day, Name = "Carols" });

        RegionChangeCheck check = await _host.WithServiceAsync(maps => maps.CheckChangeAsync(new RegionChange { AreaId = _area.Id, Off = [Home] }));

        RegionUse use = Assert.Single(check.Uses);
        Assert.Equal("Carols", use.Name);
        RegionRecord built = (await _host.RegionsAsync(_area.Id)).Single(found => found.Id == region);
        Assert.Equal(built.StreetsBytes + built.MapBytes, check.FreedBytes);
        Assert.Null(check.Problem);
    }

    [Fact]
    public async Task AnAreaKeepsAtLeastOneRegionAndGrowsOnlyFromItsEdge()
    {
        RpcException last = await Assert.ThrowsAsync<RpcException>(() => ChangeAsync(off: [Home]));
        RpcException far = await Assert.ThrowsAsync<RpcException>(() => ChangeAsync(on: [new CellRef { Row = 0, Column = 3 }]));

        Assert.Equal(StatusCode.InvalidArgument, last.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, far.StatusCode);
    }

    private Task<MapArea> AddAsync(string name, double eastMetres = 0)
    {
        GeoPoint centre = Streets.Point(eastMetres, 0);
        return _host.WithServiceAsync(maps => maps.AddAsync(new AddMapArea { Name = name, CentreLatitude = centre.Lat, CentreLongitude = centre.Lon, RadiusKm = 1.5 }));
    }

    private async Task<MapArea> AreaAsync() =>
        (await _host.WithServiceAsync(maps => maps.AreasAsync(new MapAreaListRequest()))).Areas.Single(area => area.Id == _area.Id);

    private Task<MapArea> ChangeAsync(List<CellRef>? on = null, List<CellRef>? off = null) =>
        _host.WithServiceAsync(maps => maps.ChangeRegionsAsync(new RegionChange { AreaId = _area.Id, On = on ?? [], Off = off ?? [] }));
}
