namespace Gsbc.AppPoc.Data.Outreach;

public readonly record struct GeoPoint(double Lat, double Lon);

public sealed record WalkBudget(double? Minutes = null, double? Kilometres = null);

public readonly record struct Arc(int Edge, bool Forward)
{
    public int Side => Forward ? 0 : 1;
}

public sealed record WalkLeg(string Street, IReadOnlyList<Arc> Arcs, int Letterboxes, double Metres, bool Part = false)
{
    public const string Unnamed = "Unnamed street";

    public bool Delivers => Letterboxes > 0;

    public string Title => Part ? $"{Street} (part)" : Street;
}

public sealed record WalkHop(int From, IReadOnlyList<int> Edges, int Target, double Metres);

public sealed record WalkPlan(int StartNode, IReadOnlyList<Arc> Arcs, IReadOnlyList<WalkLeg> Legs, double Metres, int Letterboxes, TimeSpan Time, double ConnectorMetres, double LongestConnectorMetres)
{
    public const int BundleSize = 50;
    public const double MaxConnectorMetres = 250;
    public const double MaxConnectorLegMetres = 150;

    public bool LongConnector => ConnectorMetres > MaxConnectorMetres || LongestConnectorMetres > MaxConnectorLegMetres;

    private readonly int[] deliveryLegs = Legs.Select((leg, i) => (leg, i)).Where(x => x.leg.Delivers).Select(x => x.i).ToArray();

    public IReadOnlyList<WalkLeg> Deliveries { get; } = Legs.Where(l => l.Delivers).ToList();

    public IEnumerable<int> Edges => Arcs.Select(a => a.Edge).Distinct();

    public WalkHop? Hop { get; init; }

    public double ApproachMetres => Legs.TakeWhile(l => !l.Delivers).Sum(l => l.Metres);

    public IReadOnlyList<string> WalkAlongBefore(int delivery)
    {
        if (delivery < 0 || delivery >= deliveryLegs.Length)
        {
            return [];
        }
        var from = delivery == 0 ? 0 : deliveryLegs[delivery - 1] + 1;
        return Legs.Skip(from).Take(deliveryLegs[delivery] - from)
            .Select(l => l.Street)
            .Where(street => street != WalkLeg.Unnamed && street != Deliveries[delivery].Street && (delivery == 0 || street != Deliveries[delivery - 1].Street))
            .Distinct()
            .ToList();
    }

    public int Bundles => (Letterboxes + BundleSize - 1) / BundleSize;

    public int BundlesToTake => (int)Math.Ceiling(Letterboxes * 1.1 / BundleSize);

    public IReadOnlyList<string> Streets => Deliveries.Select(l => l.Title).Distinct().ToList();
}

public delegate int LetterboxCount(int edge, int side);

public enum MarkerKind { Skip, DontLetterbox, Hop }

public sealed record WalkMarker(GeoPoint Point, MarkerKind Kind, string Label);

public sealed class RoutePlanner
{
    private const double WalkingMetresPerSecond = 4000.0 / 3600;
    private const double HouseSeconds = 8;
    private const double UnitSeconds = 3;
    private const double BudgetSlack = 1.02;
    private const double EarthMetres = 6371000;
    private const double ReachMetres = 400;
    private const int ApproachTries = 5;
    private const double SeedSpacingMetres = 200;

    private readonly OutreachMap map;
    private readonly StreetNetwork net;
    private readonly double cosLat;

    public RoutePlanner(StreetNetwork network)
    {
        net = network;
        map = network.Map;
        cosLat = network.CosLat;
    }

    public StreetNetwork Network => net;

    public GeoPoint Node(int node) => net.Node(node);

    public bool Contains(GeoPoint point) =>
        point.Lat >= map.Bounds[0] && point.Lat <= map.Bounds[2] && point.Lon >= map.Bounds[1] && point.Lon <= map.Bounds[3];

    public int NearestNode(GeoPoint point, IEnumerable<int>? within = null)
    {
        var candidates = within?.SelectMany(e => new[] { net[e].U, net[e].V })
            ?? net.CornerNodes.Where(n => net.Adjacent(n).Any(e => !net[e].IsArterial));
        return candidates.MinBy(n => Distance(net.Node(n), point));
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
            if (net[e].IsArterial)
            {
                continue;
            }
            var line = net[e].Line;
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

    public IEnumerable<int> NeighbourEdges(int edge) => net.Adjacent(net[edge].U).Concat(net.Adjacent(net[edge].V));

    public GeoPoint Midpoint(int edge)
    {
        var piece = net[edge];
        var middle = piece.Partial ? net.Slice(piece.Street, (piece.From + piece.To) / 2, (piece.From + piece.To) / 2)[0] : piece.Line[piece.Line.Length / 2];
        return new(middle[1], middle[0]);
    }

    public WalkPlan Plan(int start, WalkBudget budget, Func<int, bool> rested, LetterboxCount count, WalkHop? hop = null)
    {
        var plans = ApproachSeeds(start, rested, count).Select(seed => Grow(start, seed, budget, rested, count, hop)).ToList();
        return plans.Count == 0 ? Circuit([], start, count) : plans.MaxBy(p => p.Letterboxes)!;
    }

    private IEnumerable<(int Edge, List<int> Approach)> ApproachSeeds(int start, Func<int, bool> rested, LetterboxCount count)
    {
        var distance = new Dictionary<int, double> { [start] = 0 };
        var reachedBy = new Dictionary<int, int>();
        var queue = new PriorityQueue<int, double>();
        queue.Enqueue(start, 0);
        var seen = new HashSet<int>();
        var seeds = new List<GeoPoint>();
        while (queue.TryDequeue(out var node, out var reached) && seeds.Count < ApproachTries)
        {
            if (reached > distance[node])
            {
                continue;
            }
            foreach (var e in net.Adjacent(node))
            {
                if (seen.Add(e) && Deliverable(e, count) && !rested(e) && seeds.All(s => Distance(s, Midpoint(e)) >= SeedSpacingMetres))
                {
                    seeds.Add(Midpoint(e));
                    yield return (e, PathTo(node, reachedBy).ToList());
                }
                var next = Other(e, node);
                var nextDistance = reached + net[e].Length;
                if (nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
                {
                    distance[next] = nextDistance;
                    reachedBy[next] = e;
                    queue.Enqueue(next, nextDistance);
                }
            }
        }
    }

    public static double Asked(WalkBudget budget) => budget.Minutes is { } minutes ? minutes * 60 : (budget.Kilometres ?? 2) * 1000;

    public static double Reached(WalkBudget budget, WalkPlan plan) => budget.Minutes is null ? plan.Metres : plan.Time.TotalSeconds;

    private WalkPlan Grow(int start, (int Edge, List<int> Approach) seed, WalkBudget budget, Func<int, bool> rested, LetterboxCount count, WalkHop? hop)
    {
        var limit = Asked(budget);
        double Cost(int e) => budget.Minutes is null ? 2.0 * net[e].Length : WalkSeconds(e, count);
        double TransitCost(int e) => 2.0 * net[e].Length / (budget.Minutes is null ? 1 : WalkingMetresPerSecond);
        bool Empty(int e) => !Deliverable(e, count) || rested(e);

        var chosen = new HashSet<int>(seed.Approach) { seed.Edge };
        var transit = seed.Approach.Where(Empty).ToHashSet();
        var chosenNodes = chosen.SelectMany(e => new[] { net[e].U, net[e].V }).ToHashSet();
        var total = Cost(seed.Edge) + seed.Approach.Sum(p => Empty(p) ? TransitCost(p) : Cost(p));
        var radius = (budget.Minutes is { } walkMinutes ? walkMinutes * 60 * WalkingMetresPerSecond : (budget.Kilometres ?? 2) * 1000) / 2;

        bool TryAdd(int e)
        {
            var ends = new HashSet<int> { net[e].U, net[e].V };
            var touches = ends.Overlaps(chosenNodes);
            var path = touches ? [] : ShortestPath(chosenNodes, ends, ReachMetres);
            var empty = path.Where(Empty).ToList();
            var cost = Cost(e) + path.Sum(p => Empty(p) ? TransitCost(p) : Cost(p));
            if ((!touches && path.Count == 0) || LongestRun(path, empty) > WalkPlan.MaxConnectorLegMetres || total + cost > limit * BudgetSlack)
            {
                return false;
            }
            chosen.UnionWith(path);
            chosen.Add(e);
            transit.UnionWith(empty);
            chosenNodes.UnionWith(path.Append(e).SelectMany(p => new[] { net[p].U, net[p].V }));
            total += cost;
            return true;
        }

        bool TryHop(WalkHop jump)
        {
            var lead = chosenNodes.Contains(jump.From) ? [] : ShortestPath(chosenNodes, [jump.From], ReachMetres);
            if (!chosenNodes.Contains(jump.From) && lead.Count == 0)
            {
                return false;
            }
            var path = lead.Concat(jump.Edges).ToList();
            var cost = Cost(jump.Target) + path.Sum(p => Empty(p) ? TransitCost(p) : Cost(p));
            if (total + cost > limit * BudgetSlack)
            {
                return false;
            }
            chosen.UnionWith(path);
            chosen.Add(jump.Target);
            transit.UnionWith(path.Where(Empty));
            chosenNodes.UnionWith(path.Append(jump.Target).SelectMany(p => new[] { net[p].U, net[p].V }));
            total += cost;
            return true;
        }

        void Fill(IEnumerable<int> from)
        {
            var candidates = EdgesNear(from, radius, count).Where(e => !Empty(e) && !chosen.Contains(e)).ToList();
            var grew = true;
            while (grew && total < limit)
            {
                grew = false;
                foreach (var e in candidates.Where(e => !chosen.Contains(e)).ToList())
                {
                    grew |= total < limit && TryAdd(e);
                }
            }
        }

        Fill([net[seed.Edge].U, net[seed.Edge].V]);
        if (hop is null || total >= limit || !TryHop(hop))
        {
            return Circuit(chosen, start, count, transit);
        }
        Fill([net[hop.Target].U, net[hop.Target].V]);
        return Circuit(chosen, start, count, transit) with { Hop = hop };
    }

    private List<int> EdgesNear(IEnumerable<int> from, double radius, LetterboxCount count)
    {
        var distance = from.Distinct().ToDictionary(n => n, _ => 0.0);
        var queue = new PriorityQueue<int, double>();
        foreach (var node in distance.Keys)
        {
            queue.Enqueue(node, 0);
        }
        var seen = new HashSet<int>();
        var edges = new List<int>();
        while (queue.TryDequeue(out var node, out var reached))
        {
            if (reached > distance[node])
            {
                continue;
            }
            foreach (var e in net.Adjacent(node).OrderByDescending(e => Density(e, count)))
            {
                if (seen.Add(e))
                {
                    edges.Add(e);
                }
                var next = Other(e, node);
                var nextDistance = reached + net[e].Length;
                if (nextDistance <= radius && nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
                {
                    distance[next] = nextDistance;
                    queue.Enqueue(next, nextDistance);
                }
            }
        }
        return edges;
    }

    public WalkPlan Circuit(IEnumerable<int> edges, int start, LetterboxCount count, IReadOnlySet<int>? transit = null)
    {
        var walkOnly = transit?.ToHashSet() ?? [];
        var set = edges.Where(e => walkOnly.Contains(e) || Deliverable(e, count)).ToHashSet();
        if (set.Count > 0 && !set.Any(e => net[e].U == start || net[e].V == start))
        {
            start = NearestNode(net.Node(start), set);
        }
        var cuts = net.CutsOf(set);
        foreach (var link in Links(set, start, cuts))
        {
            if (set.Add(link))
            {
                walkOnly.Add(link);
            }
        }
        var arcsFrom = new Dictionary<int, List<Arc>>();
        foreach (var e in set)
        {
            Outgoing(arcsFrom, net[e].U).Add(new(e, true));
            Outgoing(arcsFrom, net[e].V).Add(new(e, false));
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
        return Summarise(start, route, (e, side) => walkOnly.Contains(e) ? 0 : count(e, side), walkOnly.Contains);
    }

    private IEnumerable<int> Links(HashSet<int> edges, int start, IReadOnlyDictionary<int, double[]> cuts)
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
                foreach (var e in pending.Where(e => reached.Contains(net[e].U) || reached.Contains(net[e].V)).ToList())
                {
                    pending.Remove(e);
                    reached.Add(net[e].U);
                    reached.Add(net[e].V);
                    grew = true;
                }
            }
            if (pending.Count == 0)
            {
                return links;
            }
            var targets = pending.SelectMany(e => new[] { net[e].U, net[e].V }).ToHashSet();
            var path = ShortestPath(reached, targets, cuts: cuts);
            if (path.Count == 0)
            {
                return links;
            }
            links.AddRange(path);
            foreach (var e in path)
            {
                reached.Add(net[e].U);
                reached.Add(net[e].V);
            }
        }
    }

    public (List<int> Edges, double Metres) ShortestLink(IEnumerable<int> fromEdges, IEnumerable<int> toEdges)
    {
        var (fromList, toList) = (fromEdges.ToList(), toEdges.ToList());
        var from = fromList.SelectMany(e => new[] { net[e].U, net[e].V }).ToHashSet();
        var to = toList.SelectMany(e => new[] { net[e].U, net[e].V }).ToHashSet();
        var path = ShortestPath(from, to, cuts: net.CutsOf(fromList.Concat(toList)));
        return (path, path.Sum(e => (double)net[e].Length));
    }

    private List<int> ShortestPath(HashSet<int> from, HashSet<int> to, double maxMetres = double.MaxValue, IReadOnlyDictionary<int, double[]>? cuts = null)
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
            foreach (var e in net.Adjacent(node, cuts))
            {
                var next = Other(e, node);
                var nextDistance = reached + net[e].Length;
                if (nextDistance <= maxMetres && nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
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

    private double LongestRun(List<int> path, List<int> empty)
    {
        var (longest, run) = (0.0, 0.0);
        foreach (var e in path)
        {
            run = empty.Contains(e) ? run + net[e].Length : 0;
            longest = Math.Max(longest, run);
        }
        return longest;
    }

    private WalkPlan Summarise(int start, List<Arc> route, LetterboxCount count, Func<int, bool> connector)
    {
        var (connectorMetres, longest, run) = (0.0, 0.0, 0.0);
        foreach (var arc in route)
        {
            var isConnector = connector(arc.Edge);
            run = isConnector ? run + net[arc.Edge].Length : 0;
            connectorMetres += isConnector ? net[arc.Edge].Length : 0;
            longest = Math.Max(longest, run);
        }
        var legs = new List<WalkLeg>();
        foreach (var arc in route)
        {
            var edge = net[arc.Edge];
            var street = string.IsNullOrEmpty(edge.Name) ? WalkLeg.Unnamed : edge.Name;
            var boxes = edge.IsArterial ? 0 : count(arc.Edge, arc.Side);
            var part = boxes > 0 && edge.Partial;
            if (legs.Count > 0 && legs[^1].Street == street && legs[^1].Delivers == boxes > 0)
            {
                var last = legs[^1];
                legs[^1] = last with { Arcs = [.. last.Arcs, arc], Letterboxes = last.Letterboxes + boxes, Metres = last.Metres + edge.Length, Part = last.Part || part };
            }
            else
            {
                legs.Add(new(street, [arc], boxes, edge.Length, part));
            }
        }
        var metres = route.Sum(a => (double)net[a.Edge].Length);
        var seconds = metres / WalkingMetresPerSecond + route.Sum(a => SideSeconds(a.Edge, a.Side, count));
        return new(start, route, legs, metres, legs.Sum(l => l.Letterboxes), TimeSpan.FromSeconds(seconds), connectorMetres, longest);
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
        var line = net[arc.Edge].Line;
        return arc.Forward ? Bearing(line[0], line[1]) : Bearing(line[^1], line[^2]);
    }

    private double InBearing(Arc arc)
    {
        var line = net[arc.Edge].Line;
        return arc.Forward ? Bearing(line[^2], line[^1]) : Bearing(line[1], line[0]);
    }

    private double Bearing(double[] from, double[] to) => Math.Atan2(to[1] - from[1], (to[0] - from[0]) * cosLat);

    private int From(Arc arc) => arc.Forward ? net[arc.Edge].U : net[arc.Edge].V;

    private int To(Arc arc) => arc.Forward ? net[arc.Edge].V : net[arc.Edge].U;

    private int Other(int edge, int node) => net[edge].U == node ? net[edge].V : net[edge].U;

    private IEnumerable<int> PathTo(int node, Dictionary<int, int> reachedBy)
    {
        while (reachedBy.TryGetValue(node, out var edge))
        {
            yield return edge;
            node = Other(edge, node);
        }
    }

    public bool Deliverable(int e, LetterboxCount count) => !net[e].IsArterial && count(e, 0) + count(e, 1) > 0;

    private double Density(int e, LetterboxCount count) =>
        net[e].IsArterial ? 0 : (count(e, 0) + count(e, 1)) / Math.Max(net[e].Length, 1.0);

    public double EdgeSeconds(int e, LetterboxCount count) => WalkSeconds(e, count);

    public static double WalkingSeconds(double metres) => metres / WalkingMetresPerSecond;

    public int NearestAnyNode(GeoPoint point) => net.CornerNodes.Where(n => net.Adjacent(n).Any()).MinBy(n => Distance(net.Node(n), point));

    public double ShortestMetres(int from, int to, IReadOnlyDictionary<int, double[]>? cuts = null)
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
            foreach (var e in net.Adjacent(node, cuts))
            {
                var next = Other(e, node);
                var nextDistance = reached + net[e].Length;
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
        2.0 * net[e].Length / WalkingMetresPerSecond + SideSeconds(e, 0, count) + SideSeconds(e, 1, count);

    private double SideSeconds(int e, int side, LetterboxCount count)
    {
        if (net[e].IsArterial)
        {
            return 0;
        }
        var boxes = count(e, side);
        var units = Math.Min(net[e].Units(side), boxes);
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
