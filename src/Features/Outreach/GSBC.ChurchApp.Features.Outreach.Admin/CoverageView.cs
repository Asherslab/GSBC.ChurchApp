using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Ui;
using GSBC.ChurchApp.Platform.Ui.Maps;
using RecordStretch = GSBC.ChurchApp.Features.Outreach.Contracts.Stretch;
using RecordCovered = GSBC.ChurchApp.Features.Outreach.Contracts.CoveredStretch;
using RoutingPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;
using GeoPoint = GSBC.ChurchApp.Features.Outreach.Contracts.GeoPoint;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public sealed record CoveredPiece(SideStretch Stretch, DateOnly Day, int Homes);

public sealed record CoverageChoice(string Title, IReadOnlyList<CoveredPiece> Pieces, IReadOnlyList<MapPoint>? Area, int? Street)
{
    public int Homes => Pieces.Sum(piece => piece.Homes);

    public int Sides => Pieces.Select(piece => (piece.Stretch.Street, piece.Stretch.Side)).Distinct().Count();
}

public readonly record struct CoverageTally(int Streets, int StreetsDone, int Homes, int HomesDone)
{
    public static CoverageTally operator +(CoverageTally one, CoverageTally other) =>
        new(one.Streets + other.Streets, one.StreetsDone + other.StreetsDone, one.Homes + other.Homes, one.HomesDone + other.HomesDone);
}

public sealed record CoverageRecord(StreetKey Street, RecordCovered Covered)
{
    public static async Task<IReadOnlyList<CoverageRecord>> ReadAsync(IEnumerable<StreetCoverage> streets, FrameBudget budget)
    {
        List<CoverageRecord> records = [];
        foreach (StreetCoverage street in streets)
        {
            await budget.BreatheAsync();
            if (StreetKey.TryParse(street.Street, out StreetKey key))
                records.AddRange(street.Stretches.Select(covered => new CoverageRecord(key, covered)));
        }
        return records;
    }
}

public sealed class CoverageView
{
    private const double SideOffsetMetres = 3.5;
    private const double CellDegrees = 0.005;
    private readonly StreetGraph _graph;
    private readonly DateOnly _today;
    private readonly IReadOnlyList<CoveredPiece> _pieces;
    private readonly IReadOnlyList<CoveredPiece> _owned;
    private readonly IReadOnlyList<CoverageCell> _cells;

    private CoverageView(StreetGraph graph, DateOnly today, IReadOnlyList<CoveredPiece> pieces, IReadOnlyList<CoveredPiece> owned,
        IReadOnlyList<CoverageCell> cells, IReadOnlyList<MapLine> lines)
    {
        (_graph, _today, _pieces, _owned, _cells) = (graph, today, pieces, owned, cells);
        Lines = lines;
    }

    public IReadOnlyList<MapLine> Lines { get; }

    public static async Task<CoverageView> BuildAsync(StreetGraph graph, IReadOnlyList<CoverageRecord> records, int restDays, DateOnly today,
        Func<MapPoint, bool> owns, FrameBudget budget)
    {
        restDays = Math.Max(1, restDays);
        List<CoveredPiece> pieces = [];
        foreach (CoverageRecord record in records)
        {
            await budget.BreatheAsync();
            if (EngineInputs.SideStretch(graph, record.Street, record.Covered) is { } stretch)
                pieces.Add(new CoveredPiece(stretch, ChurchTime.DayOf(record.Covered.At), HomesIn(graph, stretch)));
        }

        bool?[] owned = new bool?[graph.StreetCount];
        bool Owned(int street) => owned[street] ??= owns(StreetMiddle(graph, street));
        Dictionary<int, (int Homes, bool Done)> done = [];
        foreach (CoveredPiece piece in pieces)
            done[piece.Stretch.Street] = (done.GetValueOrDefault(piece.Stretch.Street).Homes + piece.Homes, true);

        Dictionary<(int Row, int Column), CoverageCell> cells = [];
        for (int street = 0; street < graph.StreetCount; street++)
        {
            await budget.BreatheAsync();
            if (!graph.Street(street).CanDeliver || graph.Homes(street) == 0)
                continue;
            MapPoint middle = StreetMiddle(graph, street);
            if (!(owned[street] ??= owns(middle)))
                continue;
            (int homesDone, bool isDone) = done.GetValueOrDefault(street);
            (int Row, int Column) at = ((int)Math.Floor(middle.Lat / CellDegrees), (int)Math.Floor(middle.Lon / CellDegrees));
            if (!cells.TryGetValue(at, out CoverageCell? cell))
                cells[at] = cell = new CoverageCell();
            cell.Add(middle, new CoverageTally(1, isDone ? 1 : 0, graph.Homes(street), homesDone));
        }

        List<CoveredPiece> ownedPieces = [];
        List<MapLine> lines = [];
        foreach (CoveredPiece piece in pieces)
        {
            await budget.BreatheAsync();
            if (!Owned(piece.Stretch.Street))
                continue;
            ownedPieces.Add(piece);
            lines.Add(new MapLine(Shape(graph, piece.Stretch), MapLineStyle.Covered, Age: Math.Clamp((today.DayNumber - piece.Day.DayNumber) / (double)restDays, 0, 1)));
        }
        return new CoverageView(graph, today, pieces, ownedPieces, [.. cells.Values], lines);
    }

    public CoverageTally TallyIn(MapFrame view)
    {
        CoverageTally sum = new();
        foreach (CoverageCell cell in _cells)
            sum += cell.TallyIn(view);
        return sum;
    }

    public CoverageChoice? StreetAt(MapPoint point)
    {
        if (_graph.Index.Nearest(_graph.Projection.ToFlat(new RoutingPoint(point.Lat, point.Lon)), 40, street => _graph.Street(street).CanDeliver && _graph.Homes(street) > 0) is not { } found)
            return null;
        return ForStreet(found.Street);
    }

    public CoverageChoice ForStreet(int street)
    {
        string name = _graph.Street(street).Name;
        return new CoverageChoice(name.Length > 0 ? name : "A street without a name", [.. _pieces.Where(piece => piece.Stretch.Street == street)], null, street);
    }

    public CoverageChoice InArea(IReadOnlyList<MapPoint> area)
    {
        List<GeoPoint> polygon = [.. area.Select(point => new GeoPoint { Lat = point.Lat, Lng = point.Lon })];
        return new CoverageChoice("The area you drew", [.. _owned.Where(piece => OutreachRules.Contains(polygon, Middle(piece.Stretch)))], area, null);
    }

    public IReadOnlyList<MapPoint> StreetShape(int street) => [.. _graph.ShapePoints(street).Select(Map)];

    public string? DoneBetween(CoverageChoice choice)
    {
        if (choice.Pieces.Count == 0)
            return null;
        int newest = choice.Pieces.Min(piece => _today.DayNumber - piece.Day.DayNumber);
        int oldest = choice.Pieces.Max(piece => _today.DayNumber - piece.Day.DayNumber);
        return newest == oldest ? $"done {DaysAgo(newest)}" : $"done {DaysAgo(newest)} to {DaysAgo(oldest)}";
    }

    public static List<RecordStretch> WholeStreet(StreetGraph graph, int street) =>
    [
        new() { Street = graph.KeyOf(street).ToString(), Side = StreetSide.Left, From = 0, To = OutreachRules.WholeStreet },
        new() { Street = graph.KeyOf(street).ToString(), Side = StreetSide.Right, From = 0, To = OutreachRules.WholeStreet },
    ];

    public StreetGraph Graph => _graph;

    private static string DaysAgo(int days) => days switch
    {
        0 => "today",
        1 => "yesterday",
        < 14 => $"{days}\u00a0days ago",
        _ => $"{days / 7}\u00a0weeks ago",
    };

    private static MapPoint StreetMiddle(StreetGraph graph, int street) => Map(graph.Projection.ToGeo(graph.PointAt(street, graph.Street(street).Length / 2)));

    private GeoPoint Middle(SideStretch stretch)
    {
        RoutingPoint point = _graph.Projection.ToGeo(_graph.PointAt(stretch.Street, (stretch.From + stretch.To) / 2));
        return new GeoPoint { Lat = point.Lat, Lng = point.Lon };
    }

    private static int HomesIn(StreetGraph graph, SideStretch stretch)
    {
        LetterboxRange boxes = graph.Between(stretch.Street, stretch.Side, stretch.From, stretch.To);
        int homes = 0;
        for (int box = boxes.Start; box < boxes.End; box++)
            if (graph.LetterboxKindOf(box).IsHome())
                homes++;
        return homes;
    }

    private static IReadOnlyList<MapPoint> Shape(StreetGraph graph, SideStretch stretch)
    {
        IReadOnlyList<Flat> flats = graph.ShapeBetween(stretch.Street, stretch.From, stretch.To);
        double towards = stretch.Side == Side.Left ? 1 : -1;
        List<MapPoint> points = new(flats.Count);
        for (int index = 0; index < flats.Count; index++)
        {
            Flat ahead = flats[Math.Min(index + 1, flats.Count - 1)] - flats[Math.Max(index - 1, 0)];
            Flat across = ahead.Length == 0 ? new Flat(0, 0) : new Flat(-ahead.Y, ahead.X) * (towards * SideOffsetMetres / ahead.Length);
            points.Add(Map(graph.Projection.ToGeo(flats[index] + across)));
        }
        return points;
    }

    private static MapPoint Map(RoutingPoint point) => new(point.Lat, point.Lon);

    private sealed class CoverageCell
    {
        private readonly List<(MapPoint Middle, CoverageTally Tally)> _streets = [];
        private CoverageTally _total;
        private double _south = double.MaxValue, _west = double.MaxValue, _north = double.MinValue, _east = double.MinValue;

        public void Add(MapPoint middle, CoverageTally tally)
        {
            _streets.Add((middle, tally));
            _total += tally;
            (_south, _west, _north, _east) = (Math.Min(_south, middle.Lat), Math.Min(_west, middle.Lon), Math.Max(_north, middle.Lat), Math.Max(_east, middle.Lon));
        }

        public CoverageTally TallyIn(MapFrame view)
        {
            if (_north < view.South || _south >= view.North || _east < view.West || _west >= view.East)
                return default;
            if (Within(new MapPoint(_south, _west), view) && Within(new MapPoint(_north, _east), view))
                return _total;
            CoverageTally sum = new();
            foreach ((MapPoint middle, CoverageTally tally) in _streets)
                if (Within(middle, view))
                    sum += tally;
            return sum;
        }

        private static bool Within(MapPoint point, MapFrame view) =>
            point.Lat >= view.South && point.Lat < view.North && point.Lon >= view.West && point.Lon < view.East;
    }
}
