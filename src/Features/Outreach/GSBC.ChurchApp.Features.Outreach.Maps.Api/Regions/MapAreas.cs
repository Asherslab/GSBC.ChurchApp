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
    public static readonly IReadOnlyList<string> Reserved = ["new"];

    public static (MapAreaRecord Area, List<MapCellRecord> Cells) New(string id, string name, string gridId, MapGrid grid, List<CellRef> cells, DateTime now)
    {
        MapAreaRecord area = new() { Id = id, Name = name, State = MapAreaStates.Waiting, CreatedAt = now, GridId = gridId };
        RegionGrid.SetGrid(area, grid);
        List<MapCellRecord> records = [.. cells.Select(cell => Cell(area, cell))];
        Fit(area, records);
        return (area, records);
    }

    public static MapCellRecord Cell(MapAreaRecord area, CellRef at) =>
        new() { AreaId = area.Id, SquareId = RegionGrid.RegionId(area.GridId, at.Row, at.Column), Row = at.Row, Column = at.Column, On = true };

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

    public static MapArea ToMessage(MapAreaRecord area, IReadOnlyCollection<MapCellRecord> cells, IReadOnlyDictionary<string, MapSquareRecord> squares,
        IReadOnlyDictionary<string, RegionRecord> regions, Func<string, IEnumerable<string>> alsoIn, IBlobStore blobs) => new()
    {
        Grid = RegionGrid.GridOf(area),
        Cells = cells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column).Select(cell =>
        {
            MapSquareRecord? square = squares.GetValueOrDefault(cell.SquareId);
            return new MapCell
            {
                Row = cell.Row, Column = cell.Column, RegionId = cell.SquareId, On = cell.On,
                Pending = cell.On && square?.Pending == true, Empty = cell.On && square?.Empty == true, Name = square?.Name ?? "",
                AlsoIn = [.. alsoIn(cell.SquareId)],
            };
        }).ToList(),
        CellLimit = RegionGrid.MostCells,
        StepsDone = area.StepsDone,
        Steps = area.Steps,
        Id = area.Id,
        Name = area.Name,
        Bounds = new MapBounds { South = area.South, West = area.West, North = area.North, East = area.East },
        State = area.State,
        Stage = area.Stage,
        Problem = area.Problem,
        StartedAt = area.StartedAt,
        BuiltAt = area.BuiltAt,
        Regions = cells.Where(cell => cell.On).Select(cell => regions.GetValueOrDefault(cell.SquareId)).OfType<RegionRecord>()
            .OrderBy(region => region.Id, StringComparer.Ordinal).Select(region => RegionFeed.ToMessage(region, blobs)).ToList(),
        Report = area.Report is null ? null : JsonSerializer.Deserialize<MapAreaReport>(area.Report),
    };
}
