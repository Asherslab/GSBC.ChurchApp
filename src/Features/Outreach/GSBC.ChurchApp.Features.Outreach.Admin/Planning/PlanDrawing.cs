using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Client.Planning;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Ui.Maps;
using GeoPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;

namespace GSBC.ChurchApp.Features.Outreach.Admin.Planning;

public static class PlanDrawing
{
    private const double ReachMetres = 30;

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
        IEnumerable<int> rested = planner.Selection is { } selection && !planner.RestedToo ? selection.Rested : [];
        foreach (int street in left.Concat(rested).Distinct())
            lines.Add(new MapLine(Points(network.Graph.ShapePoints(street)), MapLineStyle.Walked));
        foreach (PlannedSection section in planner.Sections.OrderBy(section => section.Id == selected))
        {
            string colour = selected is null || selected == section.Id ? RouteColours.Of(section.Colour) : RouteColours.Faded(section.Colour);
            foreach (int piece in section.Section.Pieces)
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
            GeoPoint middle = Middle(planner, section);
            string colour = selected is null || selected == section.Id ? RouteColours.Of(section.Colour) : RouteColours.Faded(section.Colour);
            string ink = selected is null || selected == section.Id ? "#ffffff" : RouteColours.Of(section.Colour);
            return new MapLabel(Point(middle), $"{section.Number} · {section.Letterboxes} · {OutreachWords.Minutes(section.Minutes)}", colour, section.Id == selected ? 1000 : section.Number, ink);
        }).ToList();

    public static IReadOnlyList<MapLine> CutPreview(DayPlanner planner, IReadOnlyList<CutPreview> cuts) =>
        cuts.SelectMany(cut => cut.Pieces.SelectMany(piece => piece.Pieces.Select(p =>
            new MapLine(Points(planner.Network.Line(p)), MapLineStyle.Route, Colour: RouteColours.Of(piece.Colour))))).ToList();

    public static IReadOnlyList<MapLine> StretchPreview(DayPlanner planner, IEnumerable<Routing.Stretch> stretches, string colour) =>
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
        IEnumerable<GeoPoint> points = planner.HasArea ? planner.Area
            : planner.Sections.SelectMany(section => section.Section.Pieces).Select(piece => planner.Network.Graph.Projection.ToGeo(planner.Network.Midpoint(piece)));
        List<MapPoint> all = [.. Points(points), Point(meeting)];
        return MapFrame.Around(all, padding: 60, maxZoom: 16);
    }

    private static GeoPoint Middle(DayPlanner planner, PlannedSection section)
    {
        WalkNetwork network = planner.Network;
        List<Flat> mids = section.Section.Pieces.Select(network.Midpoint).ToList();
        Flat centre = new(mids.Average(m => m.X), mids.Average(m => m.Y));
        Flat nearest = mids.MinBy(m => Flat.Distance(m, centre));
        return network.Graph.Projection.ToGeo(nearest);
    }
}
