namespace Gsbc.AppPoc.Data.Outreach;

public enum ReachStatus { Full, Short, Long }

public enum ReachReasonKind { Rested, PreviewEdge, BoxedIn, FarStreets, FirstStreet, NextTooLong }

public sealed record ReachReason(ReachReasonKind Kind, IReadOnlyList<string> Names, int Count = 0, double Metres = 0);

public sealed record HopChoice(WalkHop Hop, string Street, WalkPlan Plan, int Added);

public sealed record StartChoice(GeoPoint Point, double Metres, string Street, WalkPlan Plan);

public sealed record WalkReach(ReachStatus Status, IReadOnlyList<ReachReason> Reasons, HopChoice? Hop, StartChoice? Elsewhere, bool Capped = false)
{
    public static WalkReach Full { get; } = new(ReachStatus.Full, [], null, null);
}

public delegate WalkPlan PlanFrom(int start, WalkBudget budget, WalkHop? hop);

public sealed class ReachFinder(RoutePlanner planner, Func<int, bool> rested, LetterboxCount count, PlanFrom plan)
{
    public const double ShortOf = 0.9;
    public const double LongOf = 1.25;
    public const double MaxHopMetres = 600;
    private const double NearbyMetres = 250;
    private const double PreviewEdgeMetres = 300;
    private const double FarMetres = 150;
    private const double BoxedInShare = 0.85;
    private const int ReasonsShown = 2;
    private const int HopTries = 4;
    private const int StartTries = 3;
    private const int MinHopGain = 20;
    private const int MinHopIsland = 60;
    private const double MetresPerDegree = 111_320;
    private const double LongerAsk = 2;
    private const double GrowsBy = 1.15;

    private readonly StreetNetwork net = planner.Network;
    private Dictionary<int, Island>? islandOf;

    private sealed record Island(HashSet<int> Edges, HashSet<int> Nodes, int Letterboxes, double Metres, double Seconds, Dictionary<string, double> MainRoads);

    public static ReachStatus StatusOf(WalkBudget budget, WalkPlan walk)
    {
        var (asked, reached) = (RoutePlanner.Asked(budget), RoutePlanner.Reached(budget, walk));
        return walk.Letterboxes == 0 ? ReachStatus.Full
            : reached < asked * ShortOf ? ReachStatus.Short
            : reached > asked * LongOf ? ReachStatus.Long
            : ReachStatus.Full;
    }

    public bool CappedAt(int start, WalkBudget budget, WalkPlan walk) => StatusOf(budget, walk) == ReachStatus.Short && Longer(start, budget, walk) is null;

    private WalkPlan? Longer(int start, WalkBudget budget, WalkPlan walk)
    {
        var longer = budget.Minutes is { } minutes ? new WalkBudget(minutes * LongerAsk) : new WalkBudget(Kilometres: (budget.Kilometres ?? 2) * LongerAsk);
        var further = plan(start, longer, null);
        return RoutePlanner.Reached(budget, further) >= RoutePlanner.Reached(budget, walk) * GrowsBy ? further : null;
    }

    public IReadOnlyList<int> RestedNear(WalkPlan walk)
    {
        var points = walk.Edges.SelectMany(e => net[e].Line).ToList();
        if (points.Count == 0)
        {
            return [];
        }
        var box = Around(points, NearbyMetres);
        return Enumerable.Range(0, net.Map.Edges.Count)
            .Where(e => rested(e) && planner.Deliverable(e, count) && box(planner.Midpoint(e)))
            .ToList();
    }

    public WalkReach Assess(int start, GeoPoint from, WalkBudget budget, WalkPlan walk)
    {
        var status = StatusOf(budget, walk);
        if (status == ReachStatus.Full)
        {
            return WalkReach.Full;
        }
        var restedNear = RestedNear(walk);
        var reasons = new List<ReachReason>();
        var further = status == ReachStatus.Short ? Longer(start, budget, walk) : null;
        if (status == ReachStatus.Long || further is not null)
        {
            reasons.Add(further is not null ? new(ReachReasonKind.NextTooLong, [], further.Letterboxes)
                : walk.ApproachMetres >= FarMetres ? new(ReachReasonKind.FarStreets, [], Metres: walk.ApproachMetres)
                : new(ReachReasonKind.FirstStreet, [], Metres: walk.Metres));
            if (restedNear.Count > 0)
            {
                reasons.Add(new(ReachReasonKind.Rested, Names(restedNear), restedNear.Count));
            }
            return new(status, reasons, null, null);
        }
        var island = Merge(walk.Deliveries.SelectMany(l => l.Arcs).Select(a => Islands()[a.Edge]).Distinct().ToList());
        if (restedNear.Count > 0)
        {
            reasons.Add(new(ReachReasonKind.Rested, Names(restedNear), restedNear.Count));
        }
        if (AtPreviewEdge(island))
        {
            reasons.Add(new(ReachReasonKind.PreviewEdge, []));
        }
        if (walk.Letterboxes >= island.Letterboxes * BoxedInShare)
        {
            var roads = island.MainRoads.OrderByDescending(r => r.Value).Select(r => r.Key).Take(2).ToList();
            reasons.Add(new(ReachReasonKind.BoxedIn, roads, NonHomesNear(walk)));
        }
        return new(status, reasons.Take(ReasonsShown).ToList(), FindHop(start, budget, walk, island), FindStart(from, budget, island), Capped: true);
    }

    private HopChoice? FindHop(int start, WalkBudget budget, WalkPlan walk, Island island)
    {
        var found = new List<(WalkHop Hop, Island Island)>();
        var distance = island.Nodes.ToDictionary(n => n, _ => 0.0);
        var via = new Dictionary<int, int>();
        var queue = new PriorityQueue<int, double>();
        foreach (var node in island.Nodes)
        {
            queue.Enqueue(node, 0);
        }
        var islands = Islands();
        while (found.Count < HopTries && queue.TryDequeue(out var node, out var reached))
        {
            if (reached > distance[node])
            {
                continue;
            }
            if (!island.Nodes.Contains(node) && LiveAt(node).OrderByDescending(Boxes).Cast<int?>().FirstOrDefault() is { } target)
            {
                var landing = islands[target];
                if (found.All(f => f.Island != landing))
                {
                    var path = new List<int>();
                    var back = node;
                    while (via.TryGetValue(back, out var edge))
                    {
                        path.Insert(0, edge);
                        back = net[edge].U == back ? net[edge].V : net[edge].U;
                    }
                    found.Add((new(back, path, target, reached), landing));
                }
            }
            foreach (var e in net.Adjacent(node).Where(e => !Live(e)))
            {
                var next = net[e].U == node ? net[e].V : net[e].U;
                var nextDistance = reached + net[e].Length;
                if (nextDistance <= MaxHopMetres && nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
                {
                    distance[next] = nextDistance;
                    via[next] = e;
                    queue.Enqueue(next, nextDistance);
                }
            }
        }
        if (found.Count == 0)
        {
            return null;
        }
        var wanted = Math.Max(MinHopIsland, walk.Letterboxes * (RoutePlanner.Asked(budget) / RoutePlanner.Reached(budget, walk) - 1));
        var hop = found.Where(f => f.Island.Letterboxes >= wanted).DefaultIfEmpty(found.MaxBy(f => f.Island.Letterboxes)).First().Hop;
        var further = plan(start, budget, hop);
        var added = further.Letterboxes - walk.Letterboxes;
        return further.Hop is null || added < MinHopGain ? null : new(hop, net[hop.Target].Name, further, added);
    }

    private StartChoice? FindStart(GeoPoint from, WalkBudget budget, Island current)
    {
        var asked = RoutePlanner.Asked(budget) * ShortOf;
        var options = Islands().Values.Distinct()
            .Where(i => !current.Edges.Contains(i.Edges.First()) && (budget.Minutes is null ? i.Metres : i.Seconds) >= asked)
            .Select(i => (Island: i, Node: i.Nodes.Where(n => !net.IsVirtual(n) && LiveAt(n).Any()).MinBy(n => planner.Distance(net.Node(n), from))))
            .Select(x => (x.Island, x.Node, Metres: planner.Distance(net.Node(x.Node), from)))
            .OrderBy(x => x.Metres)
            .Take(StartTries);
        foreach (var (island, node, metres) in options)
        {
            var walk = plan(node, budget, null);
            if (StatusOf(budget, walk) != ReachStatus.Short)
            {
                return new(net.Node(node), metres, net[LiveAt(node).MaxBy(Boxes)].Name, walk);
            }
        }
        return null;
    }

    private Dictionary<int, Island> Islands()
    {
        if (islandOf is not null)
        {
            return islandOf;
        }
        islandOf = [];
        for (var e = 0; e < net.Map.Edges.Count; e++)
        {
            if (Live(e) && !islandOf.ContainsKey(e))
            {
                var island = Flood([net[e].U, net[e].V]);
                foreach (var edge in island.Edges)
                {
                    islandOf[edge] = island;
                }
            }
        }
        return islandOf;
    }

    private static Island Merge(List<Island> islands)
    {
        if (islands.Count == 1)
        {
            return islands[0];
        }
        var roads = islands.SelectMany(i => i.MainRoads).GroupBy(r => r.Key).ToDictionary(g => g.Key, g => g.Sum(r => r.Value));
        return new([.. islands.SelectMany(i => i.Edges)], [.. islands.SelectMany(i => i.Nodes)], islands.Sum(i => i.Letterboxes), islands.Sum(i => i.Metres), islands.Sum(i => i.Seconds), roads);
    }

    private Island Flood(IEnumerable<int> seeds)
    {
        var nodes = new HashSet<int>();
        var edges = new HashSet<int>();
        var roads = new Dictionary<string, double>();
        var queue = new Queue<int>();
        foreach (var seed in seeds.Where(nodes.Add))
        {
            queue.Enqueue(seed);
        }
        while (queue.TryDequeue(out var node))
        {
            foreach (var e in LiveAt(node).Where(edges.Add))
            {
                foreach (var end in new[] { net[e].U, net[e].V }.Where(nodes.Add))
                {
                    queue.Enqueue(end);
                }
            }
            foreach (var next in Across(node, roads).Where(nodes.Add))
            {
                queue.Enqueue(next);
            }
        }
        return new(edges, nodes, edges.Sum(Boxes), edges.Sum(e => 2.0 * net[e].Length), edges.Sum(e => planner.EdgeSeconds(e, count)), roads);
    }

    private List<int> Across(int from, Dictionary<string, double> roads)
    {
        var distance = new Dictionary<int, double> { [from] = 0 };
        var queue = new PriorityQueue<int, double>();
        queue.Enqueue(from, 0);
        var landed = new List<int>();
        while (queue.TryDequeue(out var node, out var reached))
        {
            if (reached > distance[node])
            {
                continue;
            }
            if (node != from && LiveAt(node).Any())
            {
                landed.Add(node);
                continue;
            }
            foreach (var e in net.Adjacent(node).Where(e => !Live(e)))
            {
                if (net[e].IsArterial && net[e].Name.Length > 0)
                {
                    roads[net[e].Name] = roads.GetValueOrDefault(net[e].Name) + net[e].Length;
                }
                var next = net[e].U == node ? net[e].V : net[e].U;
                var nextDistance = reached + net[e].Length;
                if (nextDistance <= WalkPlan.MaxConnectorLegMetres && nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
                {
                    distance[next] = nextDistance;
                    queue.Enqueue(next, nextDistance);
                }
            }
        }
        return landed;
    }

    private bool AtPreviewEdge(Island island)
    {
        var bounds = net.Map.Bounds;
        var inside = Around([[bounds[1], bounds[0]], [bounds[3], bounds[2]]], -PreviewEdgeMetres);
        return island.Edges.Any(e => !inside(planner.Midpoint(e)));
    }

    private int NonHomesNear(WalkPlan walk)
    {
        var near = Around(walk.Edges.SelectMany(e => net[e].Line).ToList(), NearbyMetres);
        return (net.Map.Skips ?? []).Count(m => near(m.Point));
    }

    private Func<GeoPoint, bool> Around(IReadOnlyList<double[]> points, double marginMetres)
    {
        var latMargin = marginMetres / MetresPerDegree;
        var lonMargin = latMargin / net.CosLat;
        var (south, north) = (points.Min(p => p[1]) - latMargin, points.Max(p => p[1]) + latMargin);
        var (west, east) = (points.Min(p => p[0]) - lonMargin, points.Max(p => p[0]) + lonMargin);
        return p => p.Lat >= south && p.Lat <= north && p.Lon >= west && p.Lon <= east;
    }

    private IReadOnlyList<string> Names(IEnumerable<int> edges) =>
        edges.Select(e => net[e].Name).Where(n => n.Length > 0).Distinct().Order().ToList();

    private IEnumerable<int> LiveAt(int node) => net.Adjacent(node).Where(Live);

    private bool Live(int e) => planner.Deliverable(e, count) && !rested(e);

    private int Boxes(int e) => count(e, 0) + count(e, 1);
}
