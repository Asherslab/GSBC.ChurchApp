using GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

public sealed class RegionGridTests
{
    private static readonly GeoPoint Church = new(-27.34335, 152.96467);

    [Fact]
    public void TheAreasCentreSitsInTheMiddleOfOneRegionNotOnACornerBetweenFour()
    {
        List<RegionCell> cells = RegionGrid.Cut("church", GeoArea.Around(Church, 20_000));

        RegionCell middle = Assert.Single(cells, cell => cell.Core.Contains(Church));
        Assert.Equal(middle.Core.Centre.Lat, Church.Lat, 6);
        Assert.Equal(middle.Core.Centre.Lon, Church.Lon, 6);
    }

    [Fact]
    public void NeighbouringRegionsOverlapSoAWalkFromACoreStaysInsideItsRegion()
    {
        List<RegionCell> cells = RegionGrid.Cut("church", GeoArea.Around(Church, 20_000));

        Assert.All(cells, cell => Assert.True(cell.Bounds.WidthMetres() - cell.Core.WidthMetres() >= 2 * RegionGrid.OverlapMetres - 1));
        Assert.True(cells.Max(cell => cell.Core.WidthMetres()) <= 10_000);
    }
}
