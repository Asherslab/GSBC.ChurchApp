using GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

public sealed class QueenslandTests
{
    [Theory]
    [InlineData(-27.34, 152.96)]
    [InlineData(-26.19, 152.66)]
    [InlineData(-28.09, 153.45)]
    [InlineData(-28.66, 151.93)]
    [InlineData(-20.73, 139.49)]
    [InlineData(-25.90, 139.35)]
    [InlineData(-10.58, 142.22)]
    public void AreasCanBeAddedAnywhereInQueensland(double latitude, double longitude) =>
        Assert.True(Queensland.Contains(new GeoPoint(latitude, longitude)));

    [Theory]
    [InlineData(-28.18, 153.56)]
    [InlineData(-29.05, 152.02)]
    [InlineData(-29.47, 149.85)]
    [InlineData(-33.87, 151.21)]
    [InlineData(-12.46, 130.84)]
    [InlineData(-27.0, 140.0)]
    public void AreasCantBeAddedOverTheBorderWhereThereIsNoAddressData(double latitude, double longitude) =>
        Assert.False(Queensland.Contains(new GeoPoint(latitude, longitude)));

    [Fact]
    public async Task APreviewMarksSquaresOverTheBorderWithoutAskingForTheirAddresses()
    {
        FakeSources sources = new();
        RegionEstimates estimates = new(sources, new MemoryCache(Options.Create(new MemoryCacheOptions())));
        (MapGrid grid, List<CellRef> cells) = RegionGrid.Plan(new GeoPoint(-28.9, 151.95), 20_000);

        List<CellEstimate> found = await estimates.EstimateAsync(grid, cells, TestContext.Current.CancellationToken);

        Assert.Contains(found, cell => cell.OutsideQueensland);
        Assert.Contains(found, cell => !cell.OutsideQueensland);
        Assert.All(found.Where(cell => cell.OutsideQueensland), cell => Assert.Equal(0, cell.Addresses));
    }
}
