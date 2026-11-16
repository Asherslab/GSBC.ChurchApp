using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Ui.Maps;
using MapAreaShape = GSBC.ChurchApp.Platform.Ui.Maps.MapArea;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public enum CellLook
{
    Built,
    Empty,
    Waiting,
    Off,
    Adding,
    SwitchingOff,
    Edge,
}

public sealed record AreaCell(CellRef Cell, CellLook Look, string Name, OutreachRegion? Region, MapBounds Core)
{
    public bool OnAfter => Look is CellLook.Built or CellLook.Empty or CellLook.Waiting or CellLook.Adding;

    public MapPoint Middle => new((Core.South + Core.North) / 2, (Core.West + Core.East) / 2);

    public long Bytes => Region is null ? 0 : Region.Streets.Bytes + Region.Map.Bytes;
}

public static class MapAreaDrawing
{
    public static List<AreaCell> Cells(Maps.Contracts.MapArea area, IReadOnlySet<CellRef> changed)
    {
        Dictionary<string, OutreachRegion> regions = area.Regions.ToDictionary(region => region.Id);
        List<AreaCell> cells = area.Cells.Select(cell =>
        {
            CellRef at = cell.Ref;
            OutreachRegion? region = regions.GetValueOrDefault(cell.RegionId);
            bool toggled = changed.Contains(at);
            CellLook look = (cell.On, toggled) switch
            {
                (true, true) => CellLook.SwitchingOff,
                (true, false) when cell.Pending => CellLook.Waiting,
                (true, false) when region is not null => CellLook.Built,
                (true, false) => cell.Empty ? CellLook.Empty : CellLook.Waiting,
                (false, true) => CellLook.Adding,
                _ => CellLook.Off,
            };
            return new AreaCell(at, look, region?.Name ?? cell.Name, region, area.Grid.Core(at.Row, at.Column));
        }).ToList();
        HashSet<CellRef> known = area.Cells.Select(cell => cell.Ref).ToHashSet();
        cells.AddRange(changed.Where(cell => !known.Contains(cell)).Select(cell => new AreaCell(cell, CellLook.Adding, "", null, area.Grid.Core(cell.Row, cell.Column))));
        if (cells.Count < area.CellLimit)
            cells.AddRange(Edges(cells).Select(cell => new AreaCell(cell, CellLook.Edge, "", null, area.Grid.Core(cell.Row, cell.Column))).ToList());
        return cells;
    }

    public static AreaCell? At(Maps.Contracts.MapArea area, IReadOnlyList<AreaCell> cells, MapPoint point)
    {
        CellRef at = area.Grid.CellAt(point.Lat, point.Lon);
        return cells.FirstOrDefault(cell => cell.Cell.Equals(at));
    }

    public static IReadOnlyList<MapAreaShape> Shapes(IReadOnlyList<AreaCell> cells, CellRef? chosen = null)
    {
        List<MapAreaShape> shapes = cells.OrderBy(cell => Layer(cell.Look)).Select(cell => Shape(cell, Ring(cell.Core))).ToList();
        if (chosen is not null && cells.FirstOrDefault(cell => cell.Cell.Equals(chosen)) is { } picked)
            shapes.Add(new MapAreaShape(Ring(picked.Core), "--ink", "chosen", Outline: true));
        return shapes;
    }

    public static IReadOnlyList<MapAreaShape> ShapesOutside(IEnumerable<AreaCell> cells, MapReach built) =>
        cells.OrderBy(cell => Layer(cell.Look))
            .SelectMany(cell => built.Outside(Box(cell.Core)).Select(piece => Shape(cell, [.. piece[0].SkipLast(1)], piece.Skip(1).ToList())))
            .ToList();

    public static MapFrame Box(MapBounds core) => new(core.South, core.West, core.North, core.East);

    private static MapAreaShape Shape(AreaCell cell, IReadOnlyList<MapPoint> ring, IReadOnlyList<IReadOnlyList<MapPoint>>? holes = null) =>
        new(ring, Colour(cell.Look), Key(cell.Cell),
            Dashed: cell.Look is CellLook.Off or CellLook.Waiting or CellLook.SwitchingOff or CellLook.Edge,
            Outline: cell.Look is CellLook.Edge or CellLook.Empty, Holes: holes);

    public static IReadOnlyList<MapLabel> Labels(IEnumerable<AreaCell> cells) =>
        cells.Select((cell, index) => new MapLabel(Named(cell.Look) ? Corner(cell.Core) : cell.Middle, MapAreaWords.Label(cell), Colour(cell.Look), index,
            cell.Look == CellLook.Adding ? MapLabelStyle.Filled : MapLabelStyle.Ring, MapAreaWords.ShortLabel(cell), Quiet: Named(cell.Look))).ToList();

    private const double NoteInset = 0.05;
    private const double NoteRoom = 100;
    private const double MarkRoom = 28;

    private const double CountRoom = 44;

    public static MapMarker Note(MapBounds core, string label) =>
        new(Box(core).InsideCorner(NoteInset), MapMarkerKind.Note, label, Corner: true, ShownFrom: Box(core).ZoomWhereItSpans(NoteRoom));

    public static IEnumerable<MapMarker> Notes(MapBounds core, string label, string? count)
    {
        MapMarker full = Note(core, label);
        yield return full;
        if (count is not null)
            yield return full with { Label = count, ShownFrom = Box(core).ZoomWhereItSpans(CountRoom), ShownUntil = full.ShownFrom };
    }

    public static MapMarker Mark(MapBounds core, MapPoint at, string label) =>
        new(at, MapMarkerKind.Note, label, ShownFrom: Box(core).ZoomWhereItSpans(MarkRoom));

    private static MapPoint Corner(MapBounds core) =>
        new(core.North - (core.North - core.South) * 0.14, core.West + (core.East - core.West) * 0.3);

    public static MapFrame Frame(IEnumerable<AreaCell> cells) =>
        MapFrame.Around(cells.SelectMany(cell => Ring(cell.Core)), padding: 44, maxZoom: 14);

    public static IReadOnlyList<MapPoint> Ring(MapBounds core) =>
    [
        new(core.North, core.West), new(core.North, core.East), new(core.South, core.East), new(core.South, core.West), new(core.North, core.West),
    ];

    private static bool Named(CellLook look) => look is CellLook.Built or CellLook.Waiting or CellLook.Empty;

    private static int Layer(CellLook look) => look switch
    {
        CellLook.Edge => 0,
        CellLook.Built or CellLook.Empty or CellLook.Waiting => 1,
        _ => 2,
    };

    private static string Colour(CellLook look) => look switch
    {
        CellLook.Built or CellLook.Waiting => "--live",
        CellLook.Adding => "--marker-ink",
        CellLook.SwitchingOff => "--danger",
        _ => "--ink-3",
    };

    public static OutreachRegion? MapFor(Maps.Contracts.MapArea area, IEnumerable<OutreachRegion> everyRegion)
    {
        MapPoint centre = new(area.Grid.CentreLatitude, area.Grid.CentreLongitude);
        return area.Regions.FirstOrDefault(region => Inside(region.Core, centre)) ?? area.Regions.FirstOrDefault() ?? Nearest(everyRegion, centre);
    }

    public static OutreachRegion? Nearest(IEnumerable<OutreachRegion> regions, MapPoint point) =>
        regions.MinBy(region => Math.Pow((region.Core.South + region.Core.North) / 2 - point.Lat, 2) + Math.Pow((region.Core.West + region.Core.East) / 2 - point.Lon, 2));

    private static bool Inside(MapBounds bounds, MapPoint point) =>
        point.Lat >= bounds.South && point.Lat <= bounds.North && point.Lon >= bounds.West && point.Lon <= bounds.East;

    private static string Key(CellRef cell) => $"{cell.Row}:{cell.Column}";

    private static IEnumerable<CellRef> Edges(List<AreaCell> cells)
    {
        HashSet<CellRef> taken = cells.Select(cell => cell.Cell).ToHashSet();
        return cells.Where(cell => cell.OnAfter)
            .SelectMany(cell => Around(cell.Cell))
            .Where(cell => !taken.Contains(cell))
            .Distinct();
    }

    private static IEnumerable<CellRef> Around(CellRef cell)
    {
        for (int row = -1; row <= 1; row++)
            for (int column = -1; column <= 1; column++)
                if (row != 0 || column != 0)
                    yield return new CellRef { Row = cell.Row + row, Column = cell.Column + column };
    }
}
