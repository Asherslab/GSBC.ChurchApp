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

public sealed class CoverageView
{
    private const double SideOffsetMetres = 3.5;
    private readonly StreetGraph _graph;
    private readonly int _restDays;
    private readonly DateOnly _today;

    public CoverageView(StreetGraph graph, IEnumerable<StreetCoverage> streets, int restDays, DateOnly today)
    {
        (_graph, _restDays, _today) = (graph, Math.Max(1, restDays), today);
        Pieces = streets.SelectMany(street => street.Stretches.Select(covered => (street.Street, covered)))
            .Select(pair => EngineInputs.SideStretch(graph, new RecordStretch { Street = pair.Street, Side = pair.covered.Side, From = pair.covered.From, To = pair.covered.To })
                is { } stretch
                ? new CoveredPiece(stretch, ChurchTime.DayOf(pair.covered.At), HomesIn(stretch))
                : null)
            .OfType<CoveredPiece>()
            .ToList();
        for (int street = 0; street < graph.StreetCount; street++)
        {
            if (!graph.Street(street).CanDeliver || graph.Homes(street) == 0)
                continue;
            DeliverableStreets++;
            AllHomes += graph.Homes(street);
        }
        StreetsDone = Pieces.Select(piece => piece.Stretch.Street).Distinct().Count();
        HomesDone = Pieces.Sum(piece => piece.Homes);
    }

    public IReadOnlyList<CoveredPiece> Pieces { get; }

    public int DeliverableStreets { get; }

    public int AllHomes { get; }

    public int StreetsDone { get; }

    public int HomesDone { get; }

    public int RestDays => _restDays;

    public IReadOnlyList<MapLine> Lines() =>
        Pieces.Select(piece => new MapLine(Shape(piece.Stretch), MapLineStyle.Covered, Age: Age(piece.Day))).ToList();

    public double Age(DateOnly day) => Math.Clamp((_today.DayNumber - day.DayNumber) / (double)_restDays, 0, 1);

    public MapFrame Frame()
    {
        GeoBounds core = _graph.Region.Core;
        return Pieces.Count > 0
            ? MapFrame.Around(Pieces.SelectMany(piece => Shape(piece.Stretch)), padding: 32, maxZoom: 16)
            : new MapFrame(core.South, core.West, core.North, core.East, 16);
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
        return new CoverageChoice("The area you drew", [.. Pieces.Where(piece => OutreachRules.Contains(polygon, Middle(piece.Stretch)))], area, null);
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
