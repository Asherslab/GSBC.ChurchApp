using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

[Integration("Two areas may include the same square: it is built once and phones get it once, it stays while any area has it on, and deleting an area keeps the squares another area has on")]
public sealed class SharedSquareTests : IAsyncLifetime
{
    private static readonly CellRef Home = new() { Row = 0, Column = 0 };
    private static readonly CellRef East = new() { Row = 0, Column = 1 };

    private MapsHost _host = null!;
    private MapArea _church = null!;

    public async ValueTask InitializeAsync()
    {
        _host = await MapsHost.StartAsync();
        _host.Sources.AddStreet("Heron Street", (3000, 0), (3300, 0));
        _host.Sources.AddHome("Heron Street", new Flat(3100, 15));
        _church = await AddAsync("Around the church", 0, 1.5);
        await _host.WithServiceAsync(maps => maps.ChangeRegionsAsync(new RegionChange { AreaId = _church.Id, On = [East] }));
        await _host.BuildAsync(_church.Id);
        _church = await AreaAsync(_church.Id);
    }

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task ASquareInTwoAreasIsBuiltOnceAndPhonesGetItOnce()
    {
        List<RegionRecord> before = await _host.AllRegionsAsync();
        Dictionary<string, int> feedRows = [];
        foreach (RegionRecord region in before)
            feedRows[region.Id] = await _host.FeedRowsAsync(region.Id);

        MapArea city = await AddAsync("Brisbane city", 3000, 3);
        await _host.BuildAsync(city.Id);

        Assert.Contains(city.Cells, cell => cell.RegionId == SquareOf(_church, East) && cell.AlsoIn.SequenceEqual(["Around the church"]));
        List<RegionRecord> after = await _host.AllRegionsAsync();
        Assert.Equal(before.Select(region => (region.Id, region.Version)), after.Select(region => (region.Id, region.Version)));
        foreach (RegionRecord region in after)
            Assert.Equal(feedRows[region.Id], await _host.FeedRowsAsync(region.Id));
        RegionList feed = await _host.WithServiceAsync(maps => maps.RegionsAsync(new RegionListRequest()));
        Assert.Equal(feed.Regions.Count, feed.Regions.Select(region => region.Id).Distinct().Count());

        MapArea built = await AreaAsync(city.Id);
        Assert.Equal(MapAreaStates.Built, built.State);
        Assert.Equal(after.Select(region => region.Id), built.Regions.Select(region => region.Id));
        Assert.Contains((await AreaAsync(_church.Id)).Cells, cell => cell.RegionId == SquareOf(_church, East) && cell.AlsoIn.SequenceEqual(["Brisbane city"]));
    }

    [Fact]
    public async Task AnAreaBeingAddedSaysWhichOfItsSquaresAnotherAreaAlreadyHas()
    {
        GeoPoint centre = Streets.Point(3000, 0);

        MapAreaPreview preview = await _host.WithServiceAsync(maps => maps.PreviewAsync(new MapAreaPlan { CentreLatitude = centre.Lat, CentreLongitude = centre.Lon, RadiusKm = 3 }));

        CellEstimate east = preview.Cells.Single(cell => cell.Row == East.Row && cell.Column == East.Column);
        RegionRecord built = (await _host.RegionsAsync(_church.Id)).Single(region => region.Id == SquareOf(_church, East));
        Assert.Equal(["Around the church"], east.AlsoIn);
        Assert.Equal(built.StreetsBytes + built.MapBytes, east.Bytes);
        Assert.Contains(preview.Cells, cell => cell.AlsoIn.Count == 0);
    }

    [Fact]
    public async Task SwitchingASquareOffInOneAreaKeepsItForTheOther()
    {
        MapArea city = await AddAsync("Brisbane city", 3000, 3);
        await _host.BuildAsync(city.Id);
        RegionRecord east = (await _host.RegionsAsync(_church.Id)).Single(region => region.Id == SquareOf(_church, East));

        RegionChangeCheck check = await _host.WithServiceAsync(maps => maps.CheckChangeAsync(new RegionChange { AreaId = _church.Id, Off = [East] }));
        await _host.WithServiceAsync(maps => maps.ChangeRegionsAsync(new RegionChange { AreaId = _church.Id, Off = [East] }));

        Assert.Equal(0, check.FreedBytes);
        Assert.DoesNotContain(await _host.RegionsAsync(_church.Id), region => region.Id == east.Id);
        Assert.Contains(await _host.RegionsAsync(city.Id), region => region.Id == east.Id && region.Version == east.Version);
        Assert.NotNull(await _host.FindAsync(east.StreetsKey));
        Assert.False((await _host.LatestFeedRowAsync(east.Id))!.Deleted);
        MapCell off = (await AreaAsync(_church.Id)).Cells.Single(cell => cell.Ref.Equals(East));
        Assert.False(off.On);
        Assert.Equal(["Brisbane city"], off.AlsoIn);
    }

    [Fact]
    public async Task DeletingAnAreaKeepsTheSquaresAnotherAreaHasOnAndDeletesTheRest()
    {
        MapArea city = await AddAsync("Brisbane city", 3000, 3);
        await _host.BuildAsync(city.Id);
        await _host.WithServiceAsync(maps => maps.ChangeRegionsAsync(new RegionChange { AreaId = city.Id, Off = [Home] }));
        List<RegionRecord> shared = await _host.RegionsAsync(city.Id);

        await _host.WithServiceAsync(maps => maps.DeleteAsync(new MapAreaRef { Id = city.Id }));

        Assert.Equal(shared.Select(region => region.Id), (await _host.RegionsAsync(_church.Id)).Where(region => shared.Any(kept => kept.Id == region.Id)).Select(region => region.Id));
        foreach (RegionRecord region in shared)
            Assert.NotNull(await _host.FindAsync(region.MapKey));

        await _host.WithServiceAsync(maps => maps.DeleteAsync(new MapAreaRef { Id = _church.Id }));

        Assert.Empty(await _host.AllRegionsAsync());
        foreach (RegionRecord region in shared)
            Assert.True((await _host.LatestFeedRowAsync(region.Id))!.Deleted);
    }

    [Fact]
    public async Task AnAreaOnAGridOfItsOwnMovesOntoTheSharedGridAndTakesInSquaresAlreadyBuilt()
    {
        MapArea city = await AddAsync("Brisbane city", 3000, 3);
        await _host.WithDbAsync(async db =>
        {
            MapAreaRecord own = await db.Areas.SingleAsync(area => area.Id == city.Id);
            (own.GridId, own.CentreLongitude) = (own.Id, own.CentreLongitude + own.CellLongitude / 3);
            foreach (MapCellRecord cell in await db.Cells.Where(cell => cell.AreaId == own.Id).ToListAsync())
            {
                db.Cells.Remove(cell);
                db.Cells.Add(new MapCellRecord { AreaId = own.Id, SquareId = RegionGrid.RegionId(own.Id, cell.Row, cell.Column), Row = cell.Row, Column = cell.Column, On = true });
            }
            await db.SaveChangesAsync();
            return 0;
        });

        await _host.WithSquaresAsync((db, squares) => MapAreaSeeding.ShareGridAsync(db, squares, _host.Clock.GetUtcNow().UtcDateTime, TestContext.Current.CancellationToken));

        MapArea moved = await AreaAsync(city.Id);
        Assert.True(RegionGrid.SameLines(_church.Grid, moved.Grid));
        Assert.All(moved.Cells, cell => Assert.StartsWith($"{_church.Id}-", cell.RegionId));
        Assert.Contains(moved.Cells, cell => cell.RegionId == SquareOf(_church, East) && !cell.Pending);
        await _host.BuildAsync(city.Id);
        Assert.Equal(MapAreaStates.Built, (await AreaAsync(city.Id)).State);
    }

    private static string SquareOf(MapArea area, CellRef cell) => area.Cells.Single(each => each.Ref.Equals(cell)).RegionId;

    private Task<MapArea> AddAsync(string name, double eastMetres, double radiusKm)
    {
        GeoPoint centre = Streets.Point(eastMetres, 0);
        return _host.WithServiceAsync(maps => maps.AddAsync(new AddMapArea { Name = name, CentreLatitude = centre.Lat, CentreLongitude = centre.Lon, RadiusKm = radiusKm }));
    }

    private async Task<MapArea> AreaAsync(string id) =>
        (await _host.WithServiceAsync(maps => maps.AreasAsync(new MapAreaListRequest()))).Areas.Single(area => area.Id == id);
}
