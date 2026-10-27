namespace Gsbc.AppPoc.Data.Outreach;

public readonly record struct GeoPoint(double Lat, double Lon);

public sealed record WalkBudget(double? Minutes = null, double? Kilometres = null);

public readonly record struct Arc(int Edge, bool Forward)
{
    public int Side => Forward ? 0 : 1;
}

public sealed record WalkLeg(string Street, IReadOnlyList<Arc> Arcs, int Letterboxes, double Metres);

public sealed record WalkPlan(int StartNode, IReadOnlyList<Arc> Arcs, IReadOnlyList<WalkLeg> Legs, double Metres, int Letterboxes, TimeSpan Time)
{
    public const int BundleSize = 50;

    public IEnumerable<int> Edges => Arcs.Select(a => a.Edge).Distinct();

    public int Bundles => (Letterboxes + BundleSize - 1) / BundleSize;

    public int BundlesToTake => (int)Math.Ceiling(Letterboxes * 1.1 / BundleSize);

    public IReadOnlyList<string> Streets => Legs.Where(l => l.Letterboxes > 0).Select(l => l.Street).Distinct().ToList();
}

public delegate int LetterboxCount(int edge, int side);

public enum MarkerKind { Skip, DontLetterbox }

public sealed record WalkMarker(GeoPoint Point, MarkerKind Kind, string Label);

public sealed class RoutePlanner
{
    private const double WalkingMetresPerSecond = 4000.0 / 3600;
    private const double HouseSeconds = 8;
    private const double UnitSeconds = 3;
    private const double BudgetSlack = 1.02;
    private const double EarthMetres = 6371000;

    private readonly OutreachMap map;
    private readonly GeoPoint[] nodes;
    private readonly List<int>[] adjacency;
    private readonly double cosLat;

    public RoutePlanner(OutreachMap map)
    {
        this.map = map;
        var count = map.Edges.Max(e => Math.Max(e.U, e.V)) + 1;
        nodes = new GeoPoint[count];
        adjacency = Enumerable.Range(0, count).Select(_ => new List<int>()).ToArray();
        for (var i = 0; i < map.Edges.Count; i++)
        {
            var edge = map.Edges[i];
            nodes[edge.U] = new(edge.Line[0][1], edge.Line[0][0]);
            nodes[edge.V] = new(edge.Line[^1][1], edge.Line[^1][0]);
            adjacency[edge.U].Add(i);
            adjacency[edge.V].Add(i);
        }
        cosLat = Math.Cos((map.Bounds[0] + map.Bounds[2]) / 2 * Math.PI / 180);
    }

    public GeoPoint Node(int node) => nodes[node];

    public bool Contains(GeoPoint point) =>
        point.Lat >= map.Bounds[0] && point.Lat <= map.Bounds[2] && point.Lon >= map.Bounds[1] && point.Lon <= map.Bounds[3];

    public int NearestNode(GeoPoint point, IEnumerable<int>? within = null)
    {
        var candidates = within?.SelectMany(e => new[] { map.Edges[e].U, map.Edges[e].V })
            ?? Enumerable.Range(0, nodes.Length).Where(n => adjacency[n].Any(e => !map.Edges[e].IsArterial));
        return candidates.MinBy(n => Distance(nodes[n], point));
    }

    public double Distance(GeoPoint a, GeoPoint b)
    {
        var dx = (b.Lon - a.Lon) * Math.PI / 180 * EarthMetres * cosLat;
        var dy = (b.Lat - a.Lat) * Math.PI / 180 * EarthMetres;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public (int Edge, int Side) NearestSide(GeoPoint point)
    {
        var best = (Edge: -1, Side: 0, Distance: double.MaxValue);
        var px = point.Lon * cosLat;
        for (var e = 0; e < map.Edges.Count; e++)
        {
            if (map.Edges[e].IsArterial)
            {
                continue;
            }
            var line = map.Edges[e].Line;
            for (var i = 0; i < line.Length - 1; i++)
            {
                var (ax, ay, bx, by) = (line[i][0] * cosLat, line[i][1], line[i + 1][0] * cosLat, line[i + 1][1]);
                var (dx, dy) = (bx - ax, by - ay);
                var t = Math.Clamp(((px - ax) * dx + (point.Lat - ay) * dy) / Math.Max(dx * dx + dy * dy, 1e-18), 0, 1);
                var distance = Math.Pow(px - (ax + t * dx), 2) + Math.Pow(point.Lat - (ay + t * dy), 2);
                if (distance < best.Distance)
                {
                    var cross = dx * (point.Lat - ay) - dy * (px - ax);
                    best = (e, cross > 0 ? 0 : 1, distance);
                }
            }
        }
        return (best.Edge, best.Side);
    }

    public IEnumerable<int> NeighbourEdges(int edge) => adjacency[map.Edges[edge].U].Concat(adjacency[map.Edges[edge].V]);

    public GeoPoint Midpoint(int edge)
    {
        var line = map.Edges[edge].Line;
        var middle = line[line.Length / 2];
        return new(middle[1], middle[0]);
    }

    public WalkPlan Plan(int start, WalkBudget budget, Func<int, bool> rested, LetterboxCount count)
    {
        var limit = budget.Minutes is { } minutes ? minutes * 60 : (budget.Kilometres ?? 2) * 1000;
        double Cost(int e) => budget.Minutes is null ? 2.0 * map.Edges[e].Length : WalkSeconds(e, count);
        double TransitCost(int e) => 2.0 * map.Edges[e].Length / (budget.Minutes is null ? 1 : WalkingMetresPerSecond);

        var distance = new Dictionary<int, double> { [start] = 0 };
        var reachedBy = new Dictionary<int, int>();
        var queue = new PriorityQueue<int, double>();
        queue.Enqueue(start, 0);
        var seen = new HashSet<int>();
        var chosen = new HashSet<int>();
        var transit = new HashSet<int>();
        var total = 0.0;

        while (queue.TryDequeue(out var node, out var reached) && total < limit)
        {
            if (reached > distance[node])
            {
                continue;
            }
            foreach (var e in adjacency[node].OrderByDescending(e => Density(e, count)))
            {
                if (!seen.Add(e))
                {
                    continue;
                }
                if (Deliverable(e, count) && !rested(e))
                {
                    var path = PathTo(node, reachedBy).Where(p => !chosen.Contains(p)).ToList();
                    var cost = Cost(e) + path.Sum(TransitCost);
                    if (total + cost <= limit * BudgetSlack)
                    {
                        chosen.UnionWith(path);
                        transit.UnionWith(path.Where(p => !Deliverable(p, count) || rested(p)));
                        chosen.Add(e);
                        total += cost;
                    }
                }
                var next = Other(e, node);
                var nextDistance = reached + map.Edges[e].Length;
                if (nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
                {
                    distance[next] = nextDistance;
                    reachedBy[next] = e;
                    queue.Enqueue(next, nextDistance);
                }
            }
        }
        return Circuit(chosen, start, count, transit);
    }

    public WalkPlan Circuit(IEnumerable<int> edges, int start, LetterboxCount count, IReadOnlySet<int>? transit = null)
    {
        var set = edges.ToHashSet();
        var walkOnly = transit?.ToHashSet() ?? [];
        if (set.Count > 0 && !set.Any(e => map.Edges[e].U == start || map.Edges[e].V == start))
        {
            start = NearestNode(nodes[start], set);
        }
        foreach (var link in Links(set, start))
        {
            if (set.Add(link))
            {
                walkOnly.Add(link);
            }
        }
        var arcsFrom = new Dictionary<int, List<Arc>>();
        foreach (var e in set)
        {
            Outgoing(arcsFrom, map.Edges[e].U).Add(new(e, true));
            Outgoing(arcsFrom, map.Edges[e].V).Add(new(e, false));
        }

        var used = new HashSet<Arc>();
        List<Arc> Tour(int from, Arc? came)
        {
            var path = new List<Arc>();
            while (NextArc(from, came, arcsFrom, used) is { } arc)
            {
                used.Add(arc);
                path.Add(arc);
                from = To(arc);
                came = arc;
            }
            return path;
        }

        var route = Tour(start, null);
        for (var i = 0; i < route.Count; i++)
        {
            var extra = Tour(From(route[i]), i > 0 ? route[i - 1] : null);
            route.InsertRange(i, extra);
        }
        foreach (var arc in arcsFrom.Values.SelectMany(a => a).Where(a => !used.Contains(a)).ToList())
        {
            if (!used.Contains(arc))
            {
                route.AddRange(Tour(From(arc), null));
            }
        }
        return Summarise(start, route, (e, side) => walkOnly.Contains(e) ? 0 : count(e, side));
    }

    private IEnumerable<int> Links(HashSet<int> edges, int start)
    {
        var reached = new HashSet<int> { start };
        var pending = edges.ToHashSet();
        var links = new List<int>();
        while (true)
        {
            var grew = true;
            while (grew)
            {
                grew = false;
                foreach (var e in pending.Where(e => reached.Contains(map.Edges[e].U) || reached.Contains(map.Edges[e].V)).ToList())
                {
                    pending.Remove(e);
                    reached.Add(map.Edges[e].U);
                    reached.Add(map.Edges[e].V);
                    grew = true;
                }
            }
            if (pending.Count == 0)
            {
                return links;
            }
            var targets = pending.SelectMany(e => new[] { map.Edges[e].U, map.Edges[e].V }).ToHashSet();
            var path = ShortestPath(reached, targets);
            if (path.Count == 0)
            {
                return links;
            }
            links.AddRange(path);
            foreach (var e in path)
            {
                reached.Add(map.Edges[e].U);
                reached.Add(map.Edges[e].V);
            }
        }
    }

    public (List<int> Edges, double Metres) ShortestLink(IEnumerable<int> fromEdges, IEnumerable<int> toEdges)
    {
        var from = fromEdges.SelectMany(e => new[] { map.Edges[e].U, map.Edges[e].V }).ToHashSet();
        var to = toEdges.SelectMany(e => new[] { map.Edges[e].U, map.Edges[e].V }).ToHashSet();
        var path = ShortestPath(from, to);
        return (path, path.Sum(e => (double)map.Edges[e].Length));
    }

    private List<int> ShortestPath(HashSet<int> from, HashSet<int> to)
    {
        var distance = new Dictionary<int, double>();
        var via = new Dictionary<int, int>();
        var queue = new PriorityQueue<int, double>();
        foreach (var node in from)
        {
            distance[node] = 0;
            queue.Enqueue(node, 0);
        }
        while (queue.TryDequeue(out var node, out var reached))
        {
            if (reached > distance[node])
            {
                continue;
            }
            if (to.Contains(node))
            {
                var path = new List<int>();
                while (via.TryGetValue(node, out var edge))
                {
                    path.Add(edge);
                    node = Other(edge, node);
                }
                return path;
            }
            foreach (var e in adjacency[node])
            {
                var next = Other(e, node);
                var nextDistance = reached + map.Edges[e].Length;
                if (nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
                {
                    distance[next] = nextDistance;
                    via[next] = e;
                    queue.Enqueue(next, nextDistance);
                }
            }
        }
        return [];
    }

    public int Letterboxes(IEnumerable<int> edges, LetterboxCount count) => edges.Sum(e => count(e, 0) + count(e, 1));

    private WalkPlan Summarise(int start, List<Arc> route, LetterboxCount count)
    {
        var legs = new List<WalkLeg>();
        foreach (var arc in route)
        {
            var edge = map.Edges[arc.Edge];
            var street = string.IsNullOrEmpty(edge.Name) ? "Unnamed street" : edge.Name;
            var boxes = edge.IsArterial ? 0 : count(arc.Edge, arc.Side);
            if (legs.Count > 0 && legs[^1].Street == street)
            {
                var last = legs[^1];
                legs[^1] = last with { Arcs = [.. last.Arcs, arc], Letterboxes = last.Letterboxes + boxes, Metres = last.Metres + edge.Length };
            }
            else
            {
                legs.Add(new(street, [arc], boxes, edge.Length));
            }
        }
        var metres = route.Sum(a => (double)map.Edges[a.Edge].Length);
        var seconds = metres / WalkingMetresPerSecond + route.Sum(a => SideSeconds(a.Edge, a.Side, count));
        return new(start, route, legs, metres, legs.Sum(l => l.Letterboxes), TimeSpan.FromSeconds(seconds));
    }

    private Arc? NextArc(int node, Arc? came, Dictionary<int, List<Arc>> arcsFrom, HashSet<Arc> used)
    {
        if (!arcsFrom.TryGetValue(node, out var options))
        {
            return null;
        }
        var open = options.Where(a => !used.Contains(a)).ToList();
        if (open.Count == 0)
        {
            return null;
        }
        if (came is not { } incoming)
        {
            return open[0];
        }
        var heading = InBearing(incoming);
        return open.MaxBy(a => a.Edge == incoming.Edge ? -Math.PI : Turn(heading, OutBearing(a)));
    }

    private static double Turn(double from, double to)
    {
        var turn = (to - from + Math.PI) % (2 * Math.PI);
        return (turn < 0 ? turn + 2 * Math.PI : turn) - Math.PI;
    }

    private double OutBearing(Arc arc)
    {
        var line = map.Edges[arc.Edge].Line;
        return arc.Forward ? Bearing(line[0], line[1]) : Bearing(line[^1], line[^2]);
    }

    private double InBearing(Arc arc)
    {
        var line = map.Edges[arc.Edge].Line;
        return arc.Forward ? Bearing(line[^2], line[^1]) : Bearing(line[1], line[0]);
    }

    private double Bearing(double[] from, double[] to) => Math.Atan2(to[1] - from[1], (to[0] - from[0]) * cosLat);

    private int From(Arc arc) => arc.Forward ? map.Edges[arc.Edge].U : map.Edges[arc.Edge].V;

    private int To(Arc arc) => arc.Forward ? map.Edges[arc.Edge].V : map.Edges[arc.Edge].U;

    private int Other(int edge, int node) => map.Edges[edge].U == node ? map.Edges[edge].V : map.Edges[edge].U;

    private IEnumerable<int> PathTo(int node, Dictionary<int, int> reachedBy)
    {
        while (reachedBy.TryGetValue(node, out var edge))
        {
            yield return edge;
            node = Other(edge, node);
        }
    }

    private bool Deliverable(int e, LetterboxCount count) => !map.Edges[e].IsArterial && count(e, 0) + count(e, 1) > 0;

    private double Density(int e, LetterboxCount count) =>
        map.Edges[e].IsArterial ? 0 : (count(e, 0) + count(e, 1)) / Math.Max(map.Edges[e].Length, 1.0);

    public double EdgeSeconds(int e, LetterboxCount count) => WalkSeconds(e, count);

    public static double WalkingSeconds(double metres) => metres / WalkingMetresPerSecond;

    public int NearestAnyNode(GeoPoint point) => Enumerable.Range(0, nodes.Length).Where(n => adjacency[n].Count > 0).MinBy(n => Distance(nodes[n], point));

    public double ShortestMetres(int from, int to)
    {
        var distance = new Dictionary<int, double> { [from] = 0 };
        var queue = new PriorityQueue<int, double>();
        queue.Enqueue(from, 0);
        while (queue.TryDequeue(out var node, out var reached))
        {
            if (node == to)
            {
                return reached;
            }
            if (reached > distance[node])
            {
                continue;
            }
            foreach (var e in adjacency[node])
            {
                var next = Other(e, node);
                var nextDistance = reached + map.Edges[e].Length;
                if (nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
                {
                    distance[next] = nextDistance;
                    queue.Enqueue(next, nextDistance);
                }
            }
        }
        return double.NaN;
    }

    private double WalkSeconds(int e, LetterboxCount count) =>
        2.0 * map.Edges[e].Length / WalkingMetresPerSecond + SideSeconds(e, 0, count) + SideSeconds(e, 1, count);

    private double SideSeconds(int e, int side, LetterboxCount count)
    {
        if (map.Edges[e].IsArterial)
        {
            return 0;
        }
        var boxes = count(e, side);
        var units = Math.Min(map.Edges[e].Units[side], boxes);
        return units * UnitSeconds + (boxes - units) * HouseSeconds;
    }

    private static List<Arc> Outgoing(Dictionary<int, List<Arc>> arcsFrom, int node)
    {
        if (!arcsFrom.TryGetValue(node, out var list))
        {
            arcsFrom[node] = list = [];
        }
        return list;
    }
}
