using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Ui.Maps;
using RecordStretch = GSBC.ChurchApp.Features.Outreach.Contracts.Stretch;
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

public sealed class CoverageView
{
    private const double SideOffsetMetres = 3.5;
    private readonly StreetGraph _graph;
    private readonly int _restDays;
    private readonly DateOnly _today;
    private readonly Func<MapPoint, bool> _owns;

    public CoverageView(StreetGraph graph, IEnumerable<StreetCoverage> streets, int restDays, DateOnly today, Func<MapPoint, bool> owns)
    {
        (_graph, _restDays, _today, _owns) = (graph, Math.Max(1, restDays), today, owns);
        Pieces = streets.SelectMany(street => street.Stretches.Select(covered => (street.Street, covered)))
            .Select(pair => EngineInputs.SideStretch(graph, new RecordStretch { Street = pair.Street, Side = pair.covered.Side, From = pair.covered.From, To = pair.covered.To })
                is { } stretch
                ? new CoveredPiece(stretch, ChurchTime.DayOf(pair.covered.At), HomesIn(stretch))
                : null)
            .OfType<CoveredPiece>()
            .ToList();
        Owned = [.. Pieces.Where(piece => _owns(StreetMiddle(piece.Stretch.Street)))];
    }

    private (int Street, MapPoint Middle)[]? _middles;

    private IReadOnlyList<CoveredPiece> Pieces { get; }

    private IReadOnlyList<CoveredPiece> Owned { get; }

    public IReadOnlyList<MapLine> Lines() =>
        Owned.Select(piece => new MapLine(Shape(piece.Stretch), MapLineStyle.Covered, Age: Age(piece.Day))).ToList();

    private double Age(DateOnly day) => Math.Clamp((_today.DayNumber - day.DayNumber) / (double)_restDays, 0, 1);

    public CoverageTally TallyIn(MapFrame view)
    {
        HashSet<int> shown = [.. Middles()
            .Where(street => Within(street.Middle, view.South, view.West, view.North, view.East) && _owns(street.Middle))
            .Select(street => street.Street)];
        List<CoveredPiece> done = [.. Pieces.Where(piece => shown.Contains(piece.Stretch.Street))];
        return new CoverageTally(shown.Count, done.Select(piece => piece.Stretch.Street).Distinct().Count(), shown.Sum(street => _graph.Homes(street)), done.Sum(piece => piece.Homes));
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
        return new CoverageChoice(name.Length > 0 ? name : "A street without a name", [.. Pieces.Where(piece => piece.Stretch.Street == street)], null, street);
    }

    public CoverageChoice InArea(IReadOnlyList<MapPoint> area)
    {
        List<GeoPoint> polygon = [.. area.Select(point => new GeoPoint { Lat = point.Lat, Lng = point.Lon })];
        return new CoverageChoice("The area you drew", [.. Owned.Where(piece => OutreachRules.Contains(polygon, Middle(piece.Stretch)))], area, null);
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

    private (int Street, MapPoint Middle)[] Middles() => _middles ??= [.. Enumerable.Range(0, _graph.StreetCount)
        .Where(street => _graph.Street(street).CanDeliver && _graph.Homes(street) > 0)
        .Select(street => (street, StreetMiddle(street)))];

    private MapPoint StreetMiddle(int street) => Map(_graph.Projection.ToGeo(_graph.PointAt(street, _graph.Street(street).Length / 2)));

    private static bool Within(MapPoint point, double south, double west, double north, double east) =>
        point.Lat >= south && point.Lat < north && point.Lon >= west && point.Lon < east;

    private GeoPoint Middle(SideStretch stretch)
    {
        RoutingPoint point = _graph.Projection.ToGeo(_graph.PointAt(stretch.Street, (stretch.From + stretch.To) / 2));
        return new GeoPoint { Lat = point.Lat, Lng = point.Lon };
    }

    private int HomesIn(SideStretch stretch)
    {
        LetterboxRange boxes = _graph.Between(stretch.Street, stretch.Side, stretch.From, stretch.To);
        int homes = 0;
        for (int box = boxes.Start; box < boxes.End; box++)
            if (_graph.LetterboxKindOf(box).IsHome())
                homes++;
        return homes;
    }

    private IReadOnlyList<MapPoint> Shape(SideStretch stretch)
    {
        IReadOnlyList<Flat> flats = _graph.ShapeBetween(stretch.Street, stretch.From, stretch.To);
        double towards = stretch.Side == Side.Left ? 1 : -1;
        List<MapPoint> points = new(flats.Count);
        for (int index = 0; index < flats.Count; index++)
        {
            Flat ahead = flats[Math.Min(index + 1, flats.Count - 1)] - flats[Math.Max(index - 1, 0)];
            Flat across = ahead.Length == 0 ? new Flat(0, 0) : new Flat(-ahead.Y, ahead.X) * (towards * SideOffsetMetres / ahead.Length);
            points.Add(Map(_graph.Projection.ToGeo(flats[index] + across)));
        }
        return points;
    }

    private static MapPoint Map(RoutingPoint point) => new(point.Lat, point.Lon);
}
