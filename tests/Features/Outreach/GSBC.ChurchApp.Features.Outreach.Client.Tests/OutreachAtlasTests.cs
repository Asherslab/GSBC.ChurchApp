using GSBC.ChurchApp.Features.Outreach.Admin;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Ui.Maps;
using MapArea = GSBC.ChurchApp.Features.Outreach.Maps.Contracts.MapArea;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class OutreachAtlasTests
{
    private static readonly MapArea Church = Area("church",
        new MapGrid { CentreLatitude = -27.34335, CentreLongitude = 152.96467, CellLatitude = 0.07186489399928178, CellLongitude = 0.08090433867274668, OriginRow = 2, OriginColumn = 2 },
        Built(3, 2, "Ashgrove and Everton Park"), Built(3, 3, "Nundah and Chermside"), Built(3, 4, "Wynnum and Wynnum West"), Built(4, 2, "Indooroopilly and Toowong"),
        Off(4, 3, "Coorparoo and New Farm"), Off(4, 4, "Carina and Carindale"));

    private static readonly MapArea SouthBrisbane = Area("south-brisbane",
        new MapGrid { CentreLatitude = -27.53269560603554, CentreLongitude = 153.1543958374433, CellLatitude = 0.05988741166606815, CellLongitude = 0.06753605997962116, OriginRow = 1, OriginColumn = 1 },
        Built(0, -1, "West End and New Farm"), Built(0, 0, "Coorparoo and Morningside"), Built(0, 1, "Manly West and Wynnum West"),
        Built(1, -1, "Annerley and Moorooka"), Built(1, 0, "Mount Gravatt East and Upper Mount Gravatt"), Empty(-1, 2), Empty(-1, 3));

    [Fact]
    public void ASquareOffOrEmptyInOneAreaNeverHidesAnotherAreasBuiltMap()
    {
        OutreachAtlas atlas = Atlas();

        foreach (OutreachRegion region in atlas.Regions)
        {
            MapPoint middle = Middle(region.Core);
            Assert.DoesNotContain(atlas.Limits.Others, shape => InShape(shape, middle));
            Assert.DoesNotContain(atlas.Limits.Notes, note => Within(region.Core, note.Point));
        }
        MapPoint carinaOutsideSouthBrisbane = new(-27.513, 153.15);
        Assert.Contains(atlas.Limits.Others, shape => InShape(shape, carinaOutsideSouthBrisbane));
        Assert.Contains(atlas.Limits.Notes, note => note.Label == "Switched off" && !atlas.Regions.Any(region => Within(region.Core, note.Point)));
    }

    [Fact]
    public void AreasWhoseSquaresOverlapMakeOneOutlineSoTheHatchingIsOneValidShape()
    {
        OutreachAtlas atlas = Atlas();

        IReadOnlyList<IReadOnlyList<IReadOnlyList<MapPoint>>> beyond = atlas.Limits.Beyond();

        Assert.Single(atlas.Limits.Edge());
        IReadOnlyList<IReadOnlyList<MapPoint>> hatching = Assert.Single(beyond);
        Assert.Equal(2, hatching.Count);
        OutreachRegion annerley = atlas.Regions.Single(region => region.Name == "Annerley and Moorooka");
        Assert.True(InRing(hatching[1], Middle(annerley.Core)));
    }

    [Fact]
    public void WithTheOverviewEveryJobPansAllOfAustraliaAndOutsideQueenslandIsClosed()
    {
        OutreachAtlas held = Atlas();
        OutreachAtlas overview = new([.. Church.Regions, .. SouthBrisbane.Regions], [Church, SouthBrisbane], Overview());

        Assert.True(held.Limits.Limit!.North < -26 && held.Limits.Limit.West > 152, "Without the overview the map is held to our regions");
        Assert.Null(held.Backdrop);
        Assert.Empty(held.Limits.Closed());
        Assert.Equal(new MapFrame(-44.5, 112.5, -9, 154), overview.Limits.Limit);
        Assert.Equal(new MapBackdrop("https://media.test/australia.pmtiles", new MapFrame(-44.5, 112.5, -9, 154), 10), overview.Backdrop);
        IReadOnlyList<IReadOnlyList<MapPoint>> closed = overview.Limits.Closed();
        Assert.Equal(2, closed.Count);
        Assert.False(InRing(closed[1], new MapPoint(-33.87, 151.21)), "Sydney is outside Queensland");
        Assert.True(InRing(closed[1], new MapPoint(-26.19, 152.66)), "Gympie is in Queensland");
        Assert.True(Turning(closed[0]) * Turning(closed[1]) < 0, "The hole winds the other way, or MapLibre fills it");
    }

    [Fact]
    public void WithNoRegionsYetTheOverviewStillShowsAMapOfQueensland()
    {
        OutreachAtlas atlas = new([], [], Overview());

        Assert.NotNull(atlas.Source);
        Assert.Empty(atlas.Source.Locations);
        Assert.True(atlas.Opening.North >= -9.5 && atlas.Opening.South <= -28.5);
        Assert.Null(new OutreachAtlas([], [], new MapOverview()).Source);
    }

    private static MapOverview Overview() => new()
    {
        Map = new MapFile { Url = "https://media.test/australia.pmtiles" },
        Bounds = new MapBounds { South = -44.5, West = 112.5, North = -9, East = 154 },
        MaxZoom = 10,
        Addable = [new MapCorner { Latitude = -9, Longitude = 138 }, new MapCorner { Latitude = -9, Longitude = 154.5 }, new MapCorner { Latitude = -28.165, Longitude = 154.5 },
            new MapCorner { Latitude = -29, Longitude = 141 }, new MapCorner { Latitude = -26, Longitude = 141 }, new MapCorner { Latitude = -26, Longitude = 138 }],
    };

    private static double Turning(IReadOnlyList<MapPoint> ring) =>
        ring.Zip(ring.Skip(1)).Sum(pair => (pair.Second.Lon - pair.First.Lon) * (pair.Second.Lat + pair.First.Lat));

    private static OutreachAtlas Atlas() => new([.. Church.Regions, .. SouthBrisbane.Regions], [Church, SouthBrisbane]);

    private static MapArea Area(string id, MapGrid grid, params MapCell[] cells)
    {
        MapArea area = new() { Id = id, Name = id, State = MapAreaStates.Built, Grid = grid, Cells = [.. cells], CellLimit = 64 };
        foreach (MapCell cell in cells.Where(cell => cell.RegionId.Length > 0))
        {
            cell.RegionId = $"{id}-{cell.Row}-{cell.Column}";
            area.Regions.Add(new OutreachRegion
            {
                Id = cell.RegionId, AreaId = id, Name = cell.Name, Core = grid.Core(cell.Row, cell.Column), Bounds = grid.Core(cell.Row, cell.Column),
                Map = new MapFile { Url = $"https://media.test/{cell.RegionId}.pmtiles" },
            });
        }
        return area;
    }

    private static MapCell Built(int row, int column, string name) => new() { Row = row, Column = column, On = true, Name = name, RegionId = "built" };

    private static MapCell Off(int row, int column, string name) => new() { Row = row, Column = column, Name = name };

    private static MapCell Empty(int row, int column) => new() { Row = row, Column = column, On = true, Empty = true };

    private static MapPoint Middle(MapBounds core) => new((core.South + core.North) / 2, (core.West + core.East) / 2);

    private static bool Within(MapBounds core, MapPoint point) =>
        point.Lat > core.South && point.Lat < core.North && point.Lon > core.West && point.Lon < core.East;

    private static bool InShape(Platform.Ui.Maps.MapArea shape, MapPoint point) =>
        InRing(shape.Points, point) && !(shape.Holes ?? []).Any(hole => InRing(hole, point));

    private static bool InRing(IReadOnlyList<MapPoint> ring, MapPoint point)
    {
        bool inside = false;
        for (int index = 0, previous = ring.Count - 1; index < ring.Count; previous = index++)
            if ((ring[index].Lat > point.Lat) != (ring[previous].Lat > point.Lat)
                && point.Lon < (ring[previous].Lon - ring[index].Lon) * (point.Lat - ring[index].Lat) / (ring[previous].Lat - ring[index].Lat) + ring[index].Lon)
                inside = !inside;
        return inside;
    }
}
