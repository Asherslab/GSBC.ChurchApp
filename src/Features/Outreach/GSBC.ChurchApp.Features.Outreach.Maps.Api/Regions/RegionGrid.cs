using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal sealed record RegionCell(string Id, GeoBounds Core, GeoBounds Bounds);

internal static class RegionGrid
{
    private const double CellMetres = 10_000;
    public const double OverlapMetres = 2_000;

    public static List<RegionCell> Cut(string areaId, GeoBounds area)
    {
        int rows = OddCount(area.HeightMetres());
        int columns = OddCount(area.WidthMetres());
        double height = (area.North - area.South) / rows;
        double width = (area.East - area.West) / columns;
        List<RegionCell> cells = [];
        for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                double north = area.North - row * height;
                double west = area.West + column * width;
                GeoBounds core = new(north - height, west, north, west + width);
                cells.Add(new RegionCell(string.Create(CultureInfo.InvariantCulture, $"{areaId}-{(char)('a' + row)}{column + 1}"), core, core.Grown(OverlapMetres)));
            }
        return cells;
    }

    private static int OddCount(double metres)
    {
        int count = Math.Max(1, (int)Math.Ceiling(metres / CellMetres - 0.05));
        return count % 2 == 0 ? count + 1 : count;
    }
}
