using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Client.Planning;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Ui.Maps;
using GeoPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;

namespace GSBC.ChurchApp.Features.Outreach.Admin.Planning;

public static class PlanDrawing
{
    private const double ReachMetres = 30;
    private const double OpeningMetres = 900;
    private const int ChosenOnTop = 10_000;
    private const int RailWidth = 64;
    private const int LabelPlaces = 24;

    public static MapPoint Point(GeoPoint point) => new(point.Lat, point.Lon);

    public static GeoPoint Point(MapPoint point) => new(point.Lat, point.Lon);

    public static IReadOnlyList<MapPoint> Points(IEnumerable<GeoPoint> points) => points.Select(Point).ToList();

    public static IReadOnlyList<GeoPoint> Points(IEnumerable<MapPoint> points) => points.Select(Point).ToList();

    public static IReadOnlyList<MapLine> Lines(DayPlanner planner, int? selected, IReadOnlyList<Contracts.Stretch>? done = null)
    {
        WalkNetwork network = planner.Network;
        List<MapLine> lines = [];
        HashSet<int> owned = planner.Draft.Sections.SelectMany(section => section.Pieces).Select(piece => network[piece].Street).ToHashSet();
        IEnumerable<int> left = planner.Draft.Streets.Where(street => !owned.Contains(street));
        foreach (int street in left.Concat(planner.LeftOutAsRested).Distinct())
            lines.Add(new MapLine(Points(network.Graph.ShapePoints(street)), MapLineStyle.Walked));
        foreach (PlannedSection section in planner.Sections.OrderBy(section => section.Id == selected))
        {
            string colour = selected is null || selected == section.Id ? RouteColours.Of(section.Colour) : RouteColours.Faded(section.Colour);
            foreach (int piece in section.WalkedThrough)
                lines.Add(new MapLine(Points(network.Line(piece)), MapLineStyle.RouteWalkedThrough, Colour: colour, Id: section.Id.ToString()));
            foreach (int piece in section.Delivered)
                lines.Add(new MapLine(Points(network.Line(piece)), MapLineStyle.Route, Colour: colour, Id: section.Id.ToString()));
        }
        foreach (Contracts.Stretch stretch in done ?? [])
            if (EngineInputs.SideStretch(network.Graph, stretch) is { } side)
                lines.Add(new MapLine(Points(network.Graph.ShapePoints(side.Street, side.From, side.To)), MapLineStyle.WalkDone));
        return lines;
    }

    public static IReadOnlyList<MapLabel> Labels(DayPlanner planner, int? selected) =>
        planner.Sections.Select(section =>
        {
            IReadOnlyList<GeoPoint> places = Places(planner, section);
            GeoPoint middle = places[0];
            bool chosen = section.Id == selected;
            string full = $"{section.Number} · {section.Letterboxes} · {OutreachWords.Minutes(section.Minutes)}{(section.Review is null ? "" : " · needs review")}";
            string brief = section.Review is null ? $"{section.Number}" : $"{section.Number} · review";
            return chosen
                ? new MapLabel(Point(middle), full, RouteColours.Of(section.Colour), ChosenOnTop, MapLabelStyle.Filled, Places: Points(places))
                : new MapLabel(Point(middle), selected is null ? full : brief, RouteColours.Of(section.Colour), section.Number, MapLabelStyle.Ring, brief, Points(places));
        }).ToList();

    public static IReadOnlyList<MapArea> Areas(DayPlanner planner)
    {
        List<MapArea> areas = [];
        foreach (DraftArea area in planner.Areas)
        {
            if (!area.OneRoute)
                areas.Add(new MapArea(Points(area.Outline), "--primary", Key(area), Dashed: true));
            else if (planner.Sections.FirstOrDefault(section => section.Area == area.Id) is { } route)
                areas.Add(new MapArea(Points(area.Outline), RouteColours.Of(route.Colour), Key(area), Outline: true));
            else
                areas.Add(new MapArea(Points(area.Outline), "--primary", Key(area), Dashed: true, Outline: true));
        }
        areas.AddRange(OverlapShapes(planner).Select(overlap => new MapArea(Points(overlap.Shape), Hatched: true)));
        return areas;
    }

    public static IReadOnlyList<MapLabel> OverlapLabels(DayPlanner planner) =>
        OverlapShapes(planner).Select(overlap => new MapLabel(Point(Middle(planner, overlap.Shape)), $"Route {overlap.KeptBy.Number} keeps these", "--ink-2", Order: -1,
            Short: $"Route {overlap.KeptBy.Number} keeps these", Places: Points(InsideCorners(planner, overlap.Shape)))).ToList();

    public static GeoPoint Top(DraftArea area) => area.Outline.MaxBy(point => point.Lat)!;

    private static string Key(DraftArea area) => $"area-{area.Id}";

    private static IEnumerable<(PlannedSection KeptBy, IReadOnlyList<GeoPoint> Shape)> OverlapShapes(DayPlanner planner)
    {
        LocalProjection projection = planner.Network.Graph.Projection;
        foreach (AreaOverlap overlap in planner.Overlaps)
        {
            if (planner.Draft.Area(overlap.KeptBy.Area) is not { } keeper)
                continue;
            List<Flat> shape = Clip(overlap.Area.Outline.Select(projection.ToFlat).ToList(), keeper.Outline.Select(projection.ToFlat).ToList());
            if (shape.Count >= 3)
                yield return (overlap.KeptBy, shape.Select(projection.ToGeo).ToList());
        }
    }

    private static IEnumerable<GeoPoint> InsideCorners(DayPlanner planner, IReadOnlyList<GeoPoint> shape)
    {
        LocalProjection projection = planner.Network.Graph.Projection;
        Flat middle = projection.ToFlat(Middle(planner, shape));
        return shape.Select(projection.ToFlat).Select(corner => projection.ToGeo(new Flat((corner.X + middle.X) / 2, (corner.Y + middle.Y) / 2)));
    }

    private static GeoPoint Middle(DayPlanner planner, IReadOnlyList<GeoPoint> shape)
    {
        LocalProjection projection = planner.Network.Graph.Projection;
        List<Flat> flat = shape.Select(projection.ToFlat).ToList();
        return projection.ToGeo(new Flat(flat.Average(f => f.X), flat.Average(f => f.Y)));
    }

    private static List<Flat> Clip(List<Flat> subject, List<Flat> clip)
    {
        if (!Convex(clip))
            (subject, clip) = Convex(subject) ? (clip, subject) : (subject, Hull(clip));
        if (Area2(clip) < 0)
            clip.Reverse();
        List<Flat> output = subject;
        for (int i = 0; i < clip.Count && output.Count > 0; i++)
        {
            (Flat a, Flat b) = (clip[i], clip[(i + 1) % clip.Count]);
            List<Flat> input = output;
            output = [];
            for (int j = 0; j < input.Count; j++)
            {
                (Flat p, Flat q) = (input[j], input[(j + 1) % input.Count]);
                bool pIn = Side(a, b, p) >= 0, qIn = Side(a, b, q) >= 0;
                if (pIn)
                    output.Add(p);
                if (pIn != qIn)
                    output.Add(Crossing(a, b, p, q));
            }
        }
        return output;
    }

    private static double Side(Flat a, Flat b, Flat p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

    private static Flat Crossing(Flat a, Flat b, Flat p, Flat q)
    {
        double t = Side(a, b, p) / (Side(a, b, p) - Side(a, b, q));
        return new Flat(p.X + (q.X - p.X) * t, p.Y + (q.Y - p.Y) * t);
    }

    private static double Area2(List<Flat> polygon) =>
        polygon.Select((p, i) => p.X * polygon[(i + 1) % polygon.Count].Y - polygon[(i + 1) % polygon.Count].X * p.Y).Sum();

    private static bool Convex(List<Flat> polygon)
    {
        int sign = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            double turn = Side(polygon[i], polygon[(i + 1) % polygon.Count], polygon[(i + 2) % polygon.Count]);
            if (Math.Abs(turn) < 1e-9)
                continue;
            if (sign != 0 && Math.Sign(turn) != sign)
                return false;
            sign = Math.Sign(turn);
        }
        return true;
    }

    private static List<Flat> Hull(List<Flat> points)
    {
        List<Flat> sorted = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        List<Flat> hull = [];
        foreach (IEnumerable<Flat> pass in new[] { sorted, Enumerable.Reverse(sorted) })
        {
            int start = hull.Count;
            foreach (Flat p in pass)
            {
                while (hull.Count >= start + 2 && Side(hull[^2], hull[^1], p) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            hull.RemoveAt(hull.Count - 1);
        }
        return hull;
    }

    public static IReadOnlyList<MapLine> CutPreview(DayPlanner planner, IReadOnlyList<CutPreview> cuts) =>
        cuts.SelectMany(cut => cut.Pieces.SelectMany(piece => piece.Pieces.Select(p =>
            new MapLine(Points(planner.Network.Line(p)), MapLineStyle.Route, Colour: RouteColours.Of(piece.Colour))))).ToList();

    public static IReadOnlyList<MapLine> StretchPreview(DayPlanner planner, IEnumerable<Stretch> stretches, string colour) =>
        stretches.Select(stretch => new MapLine(Points(planner.Network.Graph.ShapePoints(stretch.Street, stretch.From, stretch.To)), MapLineStyle.Route, Colour: colour)).ToList();

    public static (int Street, double Along)? StreetAt(DayPlanner planner, MapPoint point)
    {
        StreetGraph graph = planner.Network.Graph;
        return graph.Index.Nearest(graph.Projection.ToFlat(Point(point)), ReachMetres) is { } found ? (found.Street, found.Along) : null;
    }

    public static PlannedSection? SectionAt(DayPlanner planner, MapPoint point) =>
        StreetAt(planner, point) is { } at ? planner.OwnerOf(at.Street, at.Along) : null;

    public static MapFrame Frame(DayPlanner planner, GeoPoint meeting)
    {
        IEnumerable<GeoPoint> points = planner.Areas.Count > 0 ? planner.Areas.SelectMany(area => area.Outline)
            : planner.Sections.SelectMany(section => section.Section.Pieces).Select(piece => planner.Network.Graph.Projection.ToGeo(planner.Network.Midpoint(piece)));
        List<MapPoint> all = [.. Points(points), Point(meeting)];
        if (all.Count == 1)
        {
            Flat centre = planner.Network.Graph.Projection.ToFlat(meeting);
            all = [Point(planner.Network.Graph.Projection.ToGeo(centre + new Flat(-OpeningMetres, -OpeningMetres))),
                Point(planner.Network.Graph.Projection.ToGeo(centre + new Flat(OpeningMetres, OpeningMetres)))];
        }
        return MapFrame.Around(all, padding: 32, maxZoom: 16) with { StartPadding = RailWidth };
    }

    private static IReadOnlyList<GeoPoint> Places(DayPlanner planner, PlannedSection section)
    {
        WalkNetwork network = planner.Network;
        List<Flat> mids = section.Section.Pieces.Select(network.Midpoint).ToList();
        Flat centre = new(mids.Average(m => m.X), mids.Average(m => m.Y));
        return mids.OrderBy(m => Flat.Distance(m, centre)).Take(LabelPlaces).Select(network.Graph.Projection.ToGeo).ToList();
    }
}
