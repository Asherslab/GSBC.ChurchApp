using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal sealed record RegionCell(string Id, int Row, int Column, GeoBounds Core, GeoBounds Bounds);

internal static class RegionGrid
{
    private const double CellMetres = 10_000;
    public const double OverlapMetres = 2_000;
    public const int MostCells = 64;

    public static (MapGrid Grid, List<CellRef> Cells) Plan(GeoPoint centre, double radiusMetres) => Plan(GeoArea.Around(centre, radiusMetres));

    public static (MapGrid Grid, List<CellRef> Cells) Plan(GeoPoint centre, double radiusMetres, MapGrid shared) => Plan(GeoArea.Around(centre, radiusMetres), shared);

    public static (MapGrid Grid, List<CellRef> Cells) Plan(GeoBounds area, MapGrid shared)
    {
        (double latitude, double longitude) = (Math.Min(shared.CellLatitude / 4, (area.North - area.South) / 2), Math.Min(shared.CellLongitude / 4, (area.East - area.West) / 2));
        CellRef first = shared.CellAt(area.North - latitude, area.West + longitude);
        CellRef last = shared.CellAt(area.South + latitude, area.East - longitude);
        List<CellRef> cells = [.. Enumerable.Range(first.Row, last.Row - first.Row + 1)
            .SelectMany(row => Enumerable.Range(first.Column, last.Column - first.Column + 1).Select(column => new CellRef { Row = row, Column = column }))];
        return (Copy(shared), cells);
    }

    public static bool SameLines(MapGrid one, MapGrid other) =>
        one.CentreLatitude.Equals(other.CentreLatitude) && one.CentreLongitude.Equals(other.CentreLongitude)
        && one.CellLatitude.Equals(other.CellLatitude) && one.CellLongitude.Equals(other.CellLongitude)
        && one.OriginRow == other.OriginRow && one.OriginColumn == other.OriginColumn;

    public static (MapGrid Grid, List<CellRef> Cells) Plan(GeoBounds area)
    {
        int rows = OddCount(area.HeightMetres());
        int columns = OddCount(area.WidthMetres());
        MapGrid grid = new()
        {
            CentreLatitude = area.Centre.Lat,
            CentreLongitude = area.Centre.Lon,
            CellLatitude = (area.North - area.South) / rows,
            CellLongitude = (area.East - area.West) / columns,
            OriginRow = rows / 2,
            OriginColumn = columns / 2,
        };
        List<CellRef> cells = [];
        for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
                cells.Add(new CellRef { Row = row, Column = column });
        return (grid, cells);
    }

    public static List<RegionCell> Cut(string areaId, GeoBounds area)
    {
        (MapGrid grid, List<CellRef> cells) = Plan(area);
        return cells.Select(cell => CellOf(areaId, grid, cell.Row, cell.Column)).ToList();
    }

    public static RegionCell CellOf(string areaId, MapGrid grid, int row, int column)
    {
        GeoBounds core = Core(grid, row, column);
        return new RegionCell(RegionId(areaId, row, column), row, column, core, core.Grown(OverlapMetres));
    }

    public static GeoBounds Core(MapGrid grid, int row, int column)
    {
        MapBounds core = grid.Core(row, column);
        return new GeoBounds(core.South, core.West, core.North, core.East);
    }

    public static string RegionId(string areaId, int row, int column) =>
        row is >= 0 and < 26 && column >= 0
            ? string.Create(CultureInfo.InvariantCulture, $"{areaId}-{(char)('a' + row)}{column + 1}")
            : string.Create(CultureInfo.InvariantCulture, $"{areaId}-r{Signed(row)}c{Signed(column)}");

    private static MapGrid Copy(MapGrid grid) => new()
    {
        CentreLatitude = grid.CentreLatitude,
        CentreLongitude = grid.CentreLongitude,
        CellLatitude = grid.CellLatitude,
        CellLongitude = grid.CellLongitude,
        OriginRow = grid.OriginRow,
        OriginColumn = grid.OriginColumn,
    };

    public static MapGrid GridOf(MapAreaRecord area) => new()
    {
        CentreLatitude = area.CentreLatitude,
        CentreLongitude = area.CentreLongitude,
        CellLatitude = area.CellLatitude,
        CellLongitude = area.CellLongitude,
        OriginRow = area.OriginRow,
        OriginColumn = area.OriginColumn,
    };

    public static void SetGrid(MapAreaRecord area, MapGrid grid) =>
        (area.CentreLatitude, area.CentreLongitude, area.CellLatitude, area.CellLongitude, area.OriginRow, area.OriginColumn) =
        (grid.CentreLatitude, grid.CentreLongitude, grid.CellLatitude, grid.CellLongitude, grid.OriginRow, grid.OriginColumn);

    public static GeoBounds Union(IEnumerable<GeoBounds> bounds)
    {
        List<GeoBounds> all = bounds.ToList();
        return all.Count == 0
            ? default
            : new GeoBounds(all.Min(b => b.South), all.Min(b => b.West), all.Max(b => b.North), all.Max(b => b.East));
    }

    private static string Signed(int value) => value < 0 ? $"m{-value}" : value.ToString(CultureInfo.InvariantCulture);

    private static int OddCount(double metres)
    {
        int count = Math.Max(1, (int)Math.Ceiling(metres / CellMetres - 0.05));
        return count % 2 == 0 ? count + 1 : count;
    }
}
