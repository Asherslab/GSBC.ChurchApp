namespace Gsbc.AppPoc.Data.Outreach;

public sealed record RouteCut(DraftSection Route, HashSet<int> Kept, HashSet<int> Cut);

public sealed class RouteEditor(OutreachMap map, RoutePlanner planner)
{
    private readonly double cosLat = Math.Cos((map.Bounds[0] + map.Bounds[2]) / 2 * Math.PI / 180);

    public List<RouteCut> Cut(IEnumerable<DraftSection> sections, IReadOnlyList<GeoPoint> line)
    {
        var cut = line.Select(Flat).ToList();
        var cuts = new List<RouteCut>();
        if (cut.Count < 2)
        {
            return cuts;
        }
        foreach (var section in sections.Where(s => !s.Locked && s.Edges.Any(e => Crosses(cut, e))))
        {
            var (kept, rest) = (new HashSet<int>(), new HashSet<int>());
            foreach (var edge in section.Edges)
            {
                (LeftShare(cut, edge) >= 0.5 ? kept : rest).Add(edge);
            }
            if (kept.Count > 0 && rest.Count > 0)
            {
                cuts.Add(new(section, kept, rest));
            }
        }
        return cuts;
    }

    public int NearestNode(DraftSection section, GeoPoint point) => planner.NearestNode(point, section.Edges);

    public int LabelEdge(IReadOnlyCollection<int> edges)
    {
        var centre = new GeoPoint(edges.Average(e => planner.Midpoint(e).Lat), edges.Average(e => planner.Midpoint(e).Lon));
        return edges.MinBy(e => planner.Distance(planner.Midpoint(e), centre));
    }

    private (double X, double Y) Flat(GeoPoint p) => (p.Lon * cosLat, p.Lat);

    private (double X, double Y) Flat(double[] p) => (p[0] * cosLat, p[1]);

    private bool Crosses(List<(double X, double Y)> cut, int edge)
    {
        var line = map.Edges[edge].Line;
        for (var i = 0; i < line.Length - 1; i++)
        {
            var (a, b) = (Flat(line[i]), Flat(line[i + 1]));
            for (var j = 0; j < cut.Count - 1; j++)
            {
                if (Intersect(a, b, cut[j], cut[j + 1]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private double LeftShare(List<(double X, double Y)> cut, int edge)
    {
        var line = map.Edges[edge].Line;
        var (left, total) = (0.0, 0.0);
        for (var i = 0; i < line.Length - 1; i++)
        {
            var (a, b) = (Flat(line[i]), Flat(line[i + 1]));
            var length = Math.Max(Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y)), 1e-12);
            total += length;
            if (Side(cut, ((a.X + b.X) / 2, (a.Y + b.Y) / 2)) > 0)
            {
                left += length;
            }
        }
        return left / total;
    }

    private static double Side(List<(double X, double Y)> cut, (double X, double Y) p)
    {
        var (best, side) = (double.MaxValue, 0.0);
        for (var i = 0; i < cut.Count - 1; i++)
        {
            var (a, b) = (cut[i], cut[i + 1]);
            var (dx, dy) = (b.X - a.X, b.Y - a.Y);
            var t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / Math.Max(dx * dx + dy * dy, 1e-18), 0, 1);
            var distance = Math.Pow(p.X - (a.X + t * dx), 2) + Math.Pow(p.Y - (a.Y + t * dy), 2);
            if (distance < best)
            {
                (best, side) = (distance, dx * (p.Y - a.Y) - dy * (p.X - a.X));
            }
        }
        return side;
    }

    private static bool Intersect((double X, double Y) p1, (double X, double Y) p2, (double X, double Y) q1, (double X, double Y) q2)
    {
        static double Cross((double X, double Y) o, (double X, double Y) a, (double X, double Y) b) => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        var (d1, d2, d3, d4) = (Cross(q1, q2, p1), Cross(q1, q2, p2), Cross(p1, p2, q1), Cross(p1, p2, q2));
        return d1 * d2 < 0 && d3 * d4 < 0;
    }
}
