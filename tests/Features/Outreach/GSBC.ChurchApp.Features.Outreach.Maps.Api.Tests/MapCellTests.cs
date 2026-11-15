using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

public sealed class MapCellTests
{
    private static readonly GeoPoint Church = new(-27.34335, 152.96467);

    [Fact]
    public void AnAreaIsAnySetOfCellsSoItsMiddleCanGoAndItCanGrowOnAnyEdge()
    {
        List<CellRef> square = Cells((0, 0), (0, 1), (0, 2), (1, 0), (1, 1), (1, 2), (2, 0), (2, 1), (2, 2));

        Assert.Null(MapCells.Problem(square, square, Change(off: [(1, 1)])));
        Assert.Null(MapCells.Problem(square, square, Change(on: [(3, 3), (4, 4)])));
        Assert.Null(MapCells.Problem(square, square, Change(on: [(-1, 1)], off: [(0, 0), (0, 2)])));
    }

    [Fact]
    public void ARegionAddedAwayFromTheAreaOrTakingTheLastOneOffIsRefused()
    {
        List<CellRef> one = Cells((0, 0));

        Assert.NotNull(MapCells.Problem(one, one, Change(on: [(0, 2)])));
        Assert.NotNull(MapCells.Problem(one, one, Change(off: [(0, 0)])));
        Assert.NotNull(MapCells.Problem(one, one, Change(off: [(0, 0)], on: [(0, 1)])));
    }

    [Fact]
    public void AnAreaStopsGrowingAtItsLimit()
    {
        List<CellRef> full = Enumerable.Range(0, RegionGrid.MostCells).Select(index => new CellRef { Row = index / 8, Column = index % 8 }).ToList();

        Assert.NotNull(MapCells.Problem(full, full, Change(on: [(8, 0)])));
    }

    [Fact]
    public void RegionsAddedFarApartAreBuiltInGroupsSoNoBuildReadsTheWholeAreaBetweenThem()
    {
        List<CellRef> waiting = Cells((-1, 2), (4, 1), (4, 2), (4, 3), (-1, 3));

        List<List<CellRef>> groups = MapCells.Groups(waiting, cell => cell);

        Assert.Equal(2, groups.Count);
        Assert.Contains(groups, group => group.Count == 3 && group.All(cell => cell.Row == 4));
    }

    [Fact]
    public void AnAreaAddedOverAnotherSnapsToItsGridAndLeavesOutTheRegionsItAlreadyHas()
    {
        (MapGrid church, List<CellRef> churchCells) = RegionGrid.Plan(Church, 20_000);
        HashSet<CellRef> taken = [.. churchCells];

        (MapGrid grid, List<CellRef> cells) = RegionGrid.Plan(new GeoPoint(-27.5327, 153.1544), 15_000, church, taken);

        Assert.True(RegionGrid.SameLines(church, grid));
        Assert.NotEmpty(cells);
        Assert.DoesNotContain(cells, taken.Contains);
        Assert.Contains(cells, cell => cell.Row == churchCells.Max(each => each.Row) + 1);
        Assert.NotNull(MapCells.Problem(cells, cells, Change(on: [(4, 4)]), taken));
    }

    [Fact]
    public void ACellOutsideTheFirstGridKeepsAnIdOfItsOwn()
    {
        Assert.Equal("church-c3", RegionGrid.RegionId("church", 2, 2));
        Assert.Equal("church-rm1c2", RegionGrid.RegionId("church", -1, 2));
        Assert.Equal("church-r2cm1", RegionGrid.RegionId("church", 2, -1));
        Assert.NotEqual(RegionGrid.RegionId("church", -1, 2), RegionGrid.RegionId("church", 1, 2));
    }

    [Fact]
    public void APointFindsTheCellItIsIn()
    {
        (MapGrid grid, List<CellRef> cells) = RegionGrid.Plan(Church, 20_000);

        foreach (CellRef cell in cells)
        {
            MapBounds core = grid.Core(cell.Row, cell.Column);
            Assert.Equal(cell, grid.CellAt((core.South + core.North) / 2, (core.West + core.East) / 2));
        }
        Assert.Equal(new CellRef { Row = -1, Column = 2 }, grid.CellAt(grid.Core(0, 2).North + grid.CellLatitude / 2, Church.Lon));
    }

    private static List<CellRef> Cells(params (int Row, int Column)[] cells) => cells.Select(cell => new CellRef { Row = cell.Row, Column = cell.Column }).ToList();

    private static RegionChange Change((int, int)[]? on = null, (int, int)[]? off = null) => new()
    {
        On = Cells(on ?? []),
        Off = Cells(off ?? []),
    };
}
