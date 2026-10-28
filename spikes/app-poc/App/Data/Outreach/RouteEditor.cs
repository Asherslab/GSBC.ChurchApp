namespace Gsbc.AppPoc.Data.Outreach;

public sealed record RouteCut(DraftSection Route, HashSet<int> Kept, List<HashSet<int>> Pieces);

public sealed class RouteEditor(RoutePlanner planner)
{
    private readonly StreetNetwork net = planner.Network;
    private readonly double cosLat = planner.Network.CosLat;

    public List<RouteCut> Cut(SplitGraph graph, IEnumerable<DraftSection> sections, IReadOnlyList<GeoPoint> line, LetterboxCount count)
    {
        var cut = line.Select(Flat).ToList();
        var cuts = new List<RouteCut>();
        if (cut.Count < 2)
        {
            return cuts;
        }
        foreach (var section in sections.Where(s => !s.Locked))
        {
            var crossed = section.Edges.Select(e => (Edge: e, Along: Crossings(cut, e))).Where(c => c.Along.Count > 0).ToList();
            if (crossed.Count == 0)
            {
                continue;
            }
            var severed = crossed.Select(c => c.Edge).ToHashSet();
            var pieces = new List<HashSet<int>>();
            var untouched = new HashSet<int>();
            foreach (var whole in graph.Components(section.Edges))
            {
                var parts = graph.Components(whole.Where(e => !severed.Contains(e)).ToList()).Select(p => p.ToHashSet()).ToList();
                if (whole.Any(severed.Contains) && parts.Count > 1)
                {
                    pieces.AddRange(parts);
                }
                else
                {
                    untouched.UnionWith(whole);
                }
            }
            if (pieces.Count == 0)
            {
                continue;
            }
            foreach (var (edge, along) in crossed.Where(c => !untouched.Contains(c.Edge)))
            {
                var (near, far) = along.Min() >= 1 - along.Max() ? (net[edge].U, net[edge].V) : (net[edge].V, net[edge].U);
                var home = pieces.FirstOrDefault(p => p.Any(e => graph.Reaches(e, near)))
                    ?? pieces.FirstOrDefault(p => p.Any(e => graph.Reaches(e, far)))
                    ?? pieces.MaxBy(p => planner.Letterboxes(p, count))!;
                home.Add(edge);
            }
            var kept = pieces.MaxBy(p => planner.Letterboxes(p, count))!;
            kept.UnionWith(untouched);
            cuts.Add(new(section, kept, pieces.Where(p => p != kept && p.Count > 0).ToList()));
        }
        return cuts;
    }

    public bool Crosses(IEnumerable<DraftSection> sections, IReadOnlyList<GeoPoint> line)
    {
        var cut = line.Select(Flat).ToList();
        return cut.Count > 1 && sections.Any(s => !s.Locked && s.Edges.Any(e => Crossings(cut, e).Count > 0));
    }

    public int NearestNode(DraftSection section, GeoPoint point) => planner.NearestNode(point, section.Edges);

    public int LabelEdge(IReadOnlyCollection<int> edges)
    {
        var centre = new GeoPoint(edges.Average(e => planner.Midpoint(e).Lat), edges.Average(e => planner.Midpoint(e).Lon));
        return edges.MinBy(e => planner.Distance(planner.Midpoint(e), centre));
    }

    private (double X, double Y) Flat(GeoPoint p) => (p.Lon * cosLat, p.Lat);

    private (double X, double Y) Flat(double[] p) => (p[0] * cosLat, p[1]);

    private List<double> Crossings(List<(double X, double Y)> cut, int edge)
    {
        var line = net[edge].Line;
        var lengths = new double[line.Length - 1];
        for (var i = 0; i < lengths.Length; i++)
        {
            var (a, b) = (Flat(line[i]), Flat(line[i + 1]));
            lengths[i] = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
        }
        var total = Math.Max(lengths.Sum(), 1e-12);
        var crossings = new List<double>();
        var walked = 0.0;
        for (var i = 0; i < lengths.Length; i++)
        {
            var (a, b) = (Flat(line[i]), Flat(line[i + 1]));
            for (var j = 0; j < cut.Count - 1; j++)
            {
                if (Intersection(a, b, cut[j], cut[j + 1]) is { } t)
                {
                    crossings.Add((walked + t * lengths[i]) / total);
                }
            }
            walked += lengths[i];
        }
        return crossings;
    }

    private static double? Intersection((double X, double Y) p1, (double X, double Y) p2, (double X, double Y) q1, (double X, double Y) q2)
    {
        static double Cross((double X, double Y) o, (double X, double Y) a, (double X, double Y) b) => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        var (d1, d2, d3, d4) = (Cross(q1, q2, p1), Cross(q1, q2, p2), Cross(p1, p2, q1), Cross(p1, p2, q2));
        return d1 * d2 < 0 && d3 * d4 < 0 ? d1 / (d1 - d2) : null;
    }
}
