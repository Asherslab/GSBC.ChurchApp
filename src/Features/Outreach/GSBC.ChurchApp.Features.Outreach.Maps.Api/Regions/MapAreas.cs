using System.Text;
using System.Text.Json;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Storage;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal static class MapAreas
{
    public const double LargestRadiusKm = 30;
    public const double SmallestRadiusKm = 1;

    public static (MapAreaRecord Area, List<MapCellRecord> Cells) New(string id, string name, GeoPoint centre, double radiusMetres, DateTime now)
    {
        (MapGrid grid, List<CellRef> cells) = RegionGrid.Plan(centre, radiusMetres);
        MapAreaRecord area = new() { Id = id, Name = name, State = MapAreaStates.Waiting, CreatedAt = now };
        RegionGrid.SetGrid(area, grid);
        List<MapCellRecord> records = cells.Select(cell => new MapCellRecord
        {
            Id = RegionGrid.RegionId(id, cell.Row, cell.Column), AreaId = id, Row = cell.Row, Column = cell.Column, On = true, Pending = true,
        }).ToList();
        Fit(area, records);
        return (area, records);
    }

    public static void Fit(MapAreaRecord area, IEnumerable<MapCellRecord> cells)
    {
        MapGrid grid = RegionGrid.GridOf(area);
        GeoBounds bounds = RegionGrid.Union(cells.Where(cell => cell.On).Select(cell => RegionGrid.Core(grid, cell.Row, cell.Column)));
        (area.South, area.West, area.North, area.East) = (bounds.South, bounds.West, bounds.North, bounds.East);
    }

    public static string Slug(string name, IReadOnlySet<string> taken)
    {
        StringBuilder slug = new();
        foreach (char letter in name.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(letter))
                slug.Append(letter);
            else if (slug.Length > 0 && slug[^1] != '-')
                slug.Append('-');
        }
        string stem = slug.ToString().Trim('-') is { Length: > 0 } text ? text : "area";
        string id = stem;
        for (int number = 2; taken.Contains(id); number++)
            id = $"{stem}-{number}";
        return id;
    }

    public static MapArea ToMessage(MapAreaRecord area, IEnumerable<MapCellRecord> cells, IEnumerable<RegionRecord> regions, IBlobStore blobs) => new()
    {
        Grid = RegionGrid.GridOf(area),
        Cells = cells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column).Select(cell => new MapCell
        {
            Row = cell.Row, Column = cell.Column, RegionId = cell.Id, On = cell.On, Pending = cell.Pending, Empty = cell.Empty,
        }).ToList(),
        CellLimit = RegionGrid.MostCells,
        Id = area.Id,
        Name = area.Name,
        Bounds = new MapBounds { South = area.South, West = area.West, North = area.North, East = area.East },
        State = area.State,
        Stage = area.Stage,
        Problem = area.Problem,
        StartedAt = area.StartedAt,
        BuiltAt = area.BuiltAt,
        Regions = regions.OrderBy(region => region.Id, StringComparer.Ordinal).Select(region => RegionFeed.ToMessage(region, blobs)).ToList(),
        Report = area.Report is null ? null : JsonSerializer.Deserialize<MapAreaReport>(area.Report),
    };
}
