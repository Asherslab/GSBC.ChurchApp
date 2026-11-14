using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;

internal sealed record StreetSegment(long Way, long[] Nodes, RoadKind Kind, string Name);

internal sealed record Network(List<StreetSegment> Segments, int PathsBesideRoads);

internal static class StreetNetwork
{
    public static readonly string[] Highways =
    [
        "residential", "living_street", "unclassified", "tertiary", "secondary", "primary", "trunk",
        "trunk_link", "primary_link", "secondary_link", "tertiary_link",
        "pedestrian", "footway", "path", "cycleway", "steps",
    ];

    private const double BesideRoadMetres = 20;
    private const double ParallelDegrees = 30;
    private const double BesideShare = 0.6;
    private const double SampleMetres = 5;
    private static readonly HashSet<string> Kept = [.. Highways];
    private static readonly HashSet<string> NoAccess = ["private", "no"];
    private static readonly HashSet<string> FootAllowed = ["yes", "designated", "permissive"];
    private static readonly HashSet<string> RoadSideFootways = ["sidewalk", "crossing"];

    public static Network Build(OsmData osm, LocalProjection projection)
    {
        List<OsmWay> ways = osm.Ways.Values.Where(way => Walkable(way) && way.Nodes.All(osm.Nodes.ContainsKey)).OrderBy(way => way.Id).ToList();
        Dictionary<long, int> uses = [];
        foreach (OsmWay way in ways)
            for (int i = 0; i < way.Nodes.Length; i++)
                uses[way.Nodes[i]] = uses.GetValueOrDefault(way.Nodes[i]) + (i == 0 || i == way.Nodes.Length - 1 ? 2 : 1);

        List<StreetSegment> segments = [];
        foreach (OsmWay way in ways)
        {
            RoadKind kind = RoadKinds.FromOsm(way.Highway);
            int start = 0;
            for (int i = 1; i < way.Nodes.Length; i++)
            {
                if (uses[way.Nodes[i]] < 2 && i != way.Nodes.Length - 1)
                    continue;
                segments.Add(new StreetSegment(way.Id, way.Nodes[start..(i + 1)], kind, way.Name));
                start = i;
            }
        }
        List<StreetSegment> kept = WithoutPathsBesideRoads(segments, osm, projection);
        return new Network(Rejoined(kept), segments.Count - kept.Count);
    }

    private static List<StreetSegment> Rejoined(List<StreetSegment> segments)
    {
        Dictionary<long, int> ends = [];
        foreach (StreetSegment segment in segments)
        {
            ends[segment.Nodes[0]] = ends.GetValueOrDefault(segment.Nodes[0]) + 1;
            ends[segment.Nodes[^1]] = ends.GetValueOrDefault(segment.Nodes[^1]) + 1;
        }
        List<StreetSegment> joined = [];
        foreach (StreetSegment segment in segments)
        {
            if (joined.Count > 0 && joined[^1] is var previous && previous.Way == segment.Way && previous.Nodes[^1] == segment.Nodes[0]
                && ends[segment.Nodes[0]] == 2 && segment.Nodes[0] != previous.Nodes[0])
                joined[^1] = previous with { Nodes = [.. previous.Nodes, .. segment.Nodes[1..]] };
            else
                joined.Add(segment);
        }
        return joined;
    }

    public static bool Walkable(OsmWay way)
    {
        IReadOnlyDictionary<string, string> tags = way.Tags;
        if (!Kept.Contains(way.Highway) || way.Nodes.Length < 2 || tags.GetValueOrDefault("area") == "yes")
            return false;
        string foot = tags.GetValueOrDefault("foot", "");
        if (foot == "no" || (NoAccess.Contains(tags.GetValueOrDefault("access", "")) && !FootAllowed.Contains(foot)))
            return false;
        return !RoadSideFootways.Contains(tags.GetValueOrDefault("footway", ""));
    }

    private static List<StreetSegment> WithoutPathsBesideRoads(List<StreetSegment> segments, OsmData osm, LocalProjection projection)
    {
        SegmentGrid roads = new(BesideRoadMetres);
        foreach (StreetSegment road in segments.Where(segment => !segment.Kind.IsPath()))
            roads.Add(Shape(road, osm, projection));
        return segments.Where(segment => !segment.Kind.IsPath() || !RunsBesideARoad(Shape(segment, osm, projection), roads)).ToList();
    }

    public static bool RunsBesideARoad(Flat[] path, SegmentGrid roads)
    {
        int samples = 0;
        int beside = 0;
        for (int i = 1; i < path.Length; i++)
        {
            Flat from = path[i - 1];
            Flat step = path[i] - from;
            double length = step.Length;
            if (length == 0)
                continue;
            int count = Math.Max(1, (int)Math.Ceiling(length / SampleMetres));
            for (int k = 0; k < count; k++)
            {
                samples++;
                if (roads.Nearest(from + step * ((k + 0.5) / count)) is { Distance: <= BesideRoadMetres } nearest && Parallel(step, nearest.Direction))
                    beside++;
            }
        }
        return samples > 0 && beside >= BesideShare * samples;
    }

    private static bool Parallel(Flat a, Flat b)
    {
        double cosine = Math.Abs(Flat.Dot(a, b)) / (a.Length * b.Length);
        return cosine >= Math.Cos(ParallelDegrees * Math.PI / 180);
    }

    public static Flat[] Shape(StreetSegment segment, OsmData osm, LocalProjection projection) =>
        segment.Nodes.Select(node => projection.ToFlat(osm.Nodes[node])).ToArray();
}
