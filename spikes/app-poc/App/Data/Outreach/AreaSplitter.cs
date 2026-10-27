namespace Gsbc.AppPoc.Data.Outreach;

public enum SplitTarget { Letterboxes, Bundles, Minutes }

public sealed record AreaSelection(IReadOnlyList<int> Fresh, IReadOnlyList<int> Rested, IReadOnlyList<int> MainRoads, IReadOnlyList<int> Excluded, IReadOnlyList<int> Empty)
{
    public IEnumerable<int> Connectors(bool restedToo) => [.. MainRoads, .. Excluded, .. Empty, .. restedToo ? Rested : []];
}

public sealed class DraftSection(IEnumerable<int> edges)
{
    public HashSet<int> Edges { get; } = [.. edges];

    public string? CustomName { get; set; }
}

public sealed record SectionSummary(DraftSection Section, string Name, WalkPlan Plan, TimeSpan Time, double TravelMetres)
{
    public int Letterboxes => Plan.Letterboxes;

    public int Bundles => Plan.Bundles;

    public IEnumerable<int> WalkThrough => Plan.Edges.Where(e => !Section.Edges.Contains(e));

    public bool NeedsReview => Plan.LongConnector;
}

public sealed class SplitGraph
{
    public const double MaxJoinMetres = WalkPlan.MaxConnectorMetres / 2;

    private readonly OutreachMap map;
    private readonly Dictionary<int, int> group = [];

    public SplitGraph(OutreachMap map, IEnumerable<int> connectors, IReadOnlyCollection<int> streets)
    {
        this.map = map;
        JoinShortLinks(connectors, streets);
    }

    public bool Touch(int a, int b)
    {
        if (a == b)
        {
            return false;
        }
        var (ea, eb) = (map.Edges[a], map.Edges[b]);
        var (au, av, bu, bv) = (Find(ea.U), Find(ea.V), Find(eb.U), Find(eb.V));
        return au == bu || au == bv || av == bu || av == bv;
    }

    public void Join(IEnumerable<int> connectors)
    {
        foreach (var e in connectors)
        {
            Union(map.Edges[e].U, map.Edges[e].V);
        }
    }

    public List<List<int>> Components(IReadOnlyCollection<int> edges)
    {
        var byGroup = new Dictionary<int, List<int>>();
        foreach (var e in edges)
        {
            foreach (var g in new[] { Find(map.Edges[e].U), Find(map.Edges[e].V) }.Distinct())
            {
                if (!byGroup.TryGetValue(g, out var list))
                {
                    byGroup[g] = list = [];
                }
                list.Add(e);
            }
        }
        var seen = new HashSet<int>();
        var components = new List<List<int>>();
        foreach (var start in edges)
        {
            if (!seen.Add(start))
            {
                continue;
            }
            var component = new List<int>();
            var stack = new Stack<int>([start]);
            while (stack.TryPop(out var e))
            {
                component.Add(e);
                foreach (var next in byGroup[Find(map.Edges[e].U)].Concat(byGroup[Find(map.Edges[e].V)]))
                {
                    if (seen.Add(next))
                    {
                        stack.Push(next);
                    }
                }
            }
            components.Add(component);
        }
        return components;
    }

    private void JoinShortLinks(IEnumerable<int> connectors, IReadOnlyCollection<int> streets)
    {
        var linksAt = new Dictionary<int, List<int>>();
        foreach (var e in connectors)
        {
            foreach (var node in new[] { map.Edges[e].U, map.Edges[e].V })
            {
                if (!linksAt.TryGetValue(node, out var list))
                {
                    linksAt[node] = list = [];
                }
                list.Add(e);
            }
        }
        var ends = streets.SelectMany(e => new[] { map.Edges[e].U, map.Edges[e].V }).ToHashSet();
        foreach (var origin in ends.Where(linksAt.ContainsKey))
        {
            var distance = new Dictionary<int, double> { [origin] = 0 };
            var queue = new PriorityQueue<int, double>();
            queue.Enqueue(origin, 0);
            while (queue.TryDequeue(out var node, out var reached))
            {
                if (reached > distance[node])
                {
                    continue;
                }
                if (node != origin && ends.Contains(node))
                {
                    Union(origin, node);
                }
                foreach (var e in linksAt.GetValueOrDefault(node) ?? [])
                {
                    var next = map.Edges[e].U == node ? map.Edges[e].V : map.Edges[e].U;
                    var nextDistance = reached + map.Edges[e].Length;
                    if (nextDistance <= MaxJoinMetres && nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
                    {
                        distance[next] = nextDistance;
                        queue.Enqueue(next, nextDistance);
                    }
                }
            }
        }
    }

    private int Find(int node)
    {
        var root = node;
        while (group.TryGetValue(root, out var parent) && parent != root)
        {
            root = parent;
        }
        while (group.TryGetValue(node, out var parent) && parent != root)
        {
            group[node] = root;
            node = parent;
        }
        return root;
    }

    private void Union(int a, int b)
    {
        var (ra, rb) = (Find(a), Find(b));
        if (ra != rb)
        {
            group[ra] = rb;
        }
    }
}

public sealed class AreaSplitter(OutreachMap map, RoutePlanner planner, LetterboxCount count)
{
    private const int MaxBalanceMoves = 400;

    public AreaSelection Select(IReadOnlyList<GeoPoint> polygon, Func<int, bool> rested, Func<int, bool> excluded)
    {
        var inside = Enumerable.Range(0, map.Edges.Count).Where(e => Inside(polygon, planner.Midpoint(e))).ToList();
        var streets = inside.Where(e => !map.Edges[e].IsArterial && !excluded(e)).ToList();
        var delivering = streets.Where(e => planner.Deliverable(e, count)).ToList();
        return new(
            delivering.Where(e => !rested(e)).ToList(),
            delivering.Where(rested).ToList(),
            inside.Where(e => map.Edges[e].IsArterial).ToList(),
            inside.Where(e => !map.Edges[e].IsArterial && excluded(e)).ToList(),
            streets.Except(delivering).ToList());
    }

    public static bool Inside(IReadOnlyList<GeoPoint> polygon, GeoPoint point)
    {
        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var (a, b) = (polygon[i], polygon[j]);
            if ((a.Lat > point.Lat) != (b.Lat > point.Lat)
                && point.Lon < (b.Lon - a.Lon) * (point.Lat - a.Lat) / (b.Lat - a.Lat) + a.Lon)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    public double Weight(int edge, SplitTarget target) => target switch
    {
        SplitTarget.Minutes => planner.EdgeSeconds(edge, count) / 60,
        SplitTarget.Bundles => (count(edge, 0) + count(edge, 1)) / (double)WalkPlan.BundleSize,
        _ => count(edge, 0) + count(edge, 1),
    };

    public (List<DraftSection> Sections, SplitGraph Graph) Split(IEnumerable<int> edges, SplitTarget target, double perRoute, IEnumerable<int> connectors)
    {
        var streets = edges.Where(e => planner.Deliverable(e, count)).ToHashSet();
        var graph = new SplitGraph(map, connectors, streets);
        var total = streets.Sum(e => Weight(e, target));
        if (total <= 0 || perRoute <= 0)
        {
            return ([], graph);
        }
        LinkNearbyIslands(graph, streets);

        var islands = graph.Components(streets)
            .Select(c => (Edges: c.ToHashSet(), Weight: c.Sum(e => Weight(e, target))))
            .Where(c => c.Weight > 0)
            .OrderByDescending(c => c.Weight)
            .ToList();
        var shares = islands.Select(i => Math.Max(1, (int)Math.Round(i.Weight / perRoute))).ToArray();

        var sections = new List<DraftSection>();
        for (var i = 0; i < islands.Count; i++)
        {
            sections.AddRange(Grow(graph, islands[i].Edges, Math.Min(shares[i], islands[i].Edges.Count), target));
        }
        Balance(graph, sections, target);
        if (MergeScraps(graph, sections, target, perRoute))
        {
            Balance(graph, sections, target);
        }
        return (sections, graph);
    }

    public List<DraftSection> SplitInTwo(SplitGraph graph, DraftSection section, SplitTarget target)
    {
        var halves = Grow(graph, section.Edges, Math.Min(2, section.Edges.Count), target);
        Balance(graph, halves, target);
        return halves;
    }

    public SectionSummary Summarise(DraftSection section, GeoPoint meeting)
    {
        var plan = planner.Circuit(section.Edges, planner.NearestNode(meeting, section.Edges), count);
        var travel = planner.ShortestMetres(planner.NearestAnyNode(meeting), plan.StartNode);
        if (double.IsNaN(travel))
        {
            travel = planner.Distance(meeting, planner.Node(plan.StartNode)) * 1.3;
        }
        var time = plan.Time + TimeSpan.FromSeconds(2 * RoutePlanner.WalkingSeconds(travel));
        return new(section, section.CustomName ?? Name(section), plan, time, travel);
    }

    public string Name(DraftSection section)
    {
        var names = section.Edges
            .Where(e => !string.IsNullOrEmpty(map.Edges[e].Name))
            .GroupBy(e => map.Edges[e].Name)
            .OrderByDescending(g => g.Sum(e => count(e, 0) + count(e, 1)))
            .Take(2)
            .Select(g => g.Key);
        return string.Join(" & ", names) is { Length: > 0 } name ? name : "Unnamed streets";
    }

    public static IEnumerable<DraftSection> Neighbours(SplitGraph graph, DraftSection section, IEnumerable<DraftSection> sections) =>
        sections.Where(s => s != section && s.Edges.Any(e => section.Edges.Any(f => graph.Touch(e, f))));

    public static IEnumerable<DraftSection> NeighboursOfEdge(SplitGraph graph, int edge, DraftSection owner, IEnumerable<DraftSection> sections) =>
        sections.Where(s => s != owner && s.Edges.Any(e => graph.Touch(e, edge)));

    public static bool CanGive(SplitGraph graph, DraftSection from, int edge) =>
        from.Edges.Count > 1 && graph.Components(from.Edges.Where(e => e != edge).ToList()).Count <= graph.Components(from.Edges).Count;

    private bool MergeScraps(SplitGraph graph, List<DraftSection> sections, SplitTarget target, double perRoute)
    {
        double Total(DraftSection s) => s.Edges.Sum(e => Weight(e, target));
        var merged = false;
        while (sections.Count > 1 && sections.MinBy(Total) is { } scrap && Total(scrap) < perRoute / 3)
        {
            if (Neighbours(graph, scrap, sections).MinBy(Total) is not { } into)
            {
                break;
            }
            into.Edges.UnionWith(scrap.Edges);
            sections.Remove(scrap);
            merged = true;
        }
        return merged;
    }

    private void LinkNearbyIslands(SplitGraph graph, HashSet<int> streets)
    {
        while (graph.Components(streets) is { Count: > 1 } islands)
        {
            var best = islands
                .Select(island => planner.ShortestLink(island, streets.Except(island)))
                .Where(link => link.Edges.Count > 0)
                .DefaultIfEmpty()
                .MinBy(link => link.Edges is null ? double.MaxValue : link.Metres);
            if (best.Edges is null || best.Metres > SplitGraph.MaxJoinMetres)
            {
                return;
            }
            graph.Join(best.Edges);
        }
    }

    private List<DraftSection> Grow(SplitGraph graph, IReadOnlyCollection<int> edges, int sections, SplitTarget target)
    {
        var seeds = Seeds(edges, sections, target);
        var totals = seeds.Select(s => Weight(s, target)).ToArray();
        var members = seeds.Select(s => new HashSet<int> { s }).ToList();
        var open = Enumerable.Range(0, seeds.Count).ToHashSet();
        var unassigned = edges.Except(seeds).ToHashSet();
        while (unassigned.Count > 0 && open.Count > 0)
        {
            var lightest = open.MinBy(i => totals[i]);
            var seedPoint = planner.Midpoint(seeds[lightest]);
            var next = unassigned
                .Where(e => members[lightest].Any(m => graph.Touch(e, m)))
                .OrderBy(e => planner.Distance(planner.Midpoint(e), seedPoint))
                .Select(e => (int?)e)
                .FirstOrDefault();
            if (next is not { } edge)
            {
                open.Remove(lightest);
                continue;
            }
            unassigned.Remove(edge);
            members[lightest].Add(edge);
            totals[lightest] += Weight(edge, target);
        }
        foreach (var edge in unassigned)
        {
            members[Enumerable.Range(0, seeds.Count).MinBy(i => planner.Distance(planner.Midpoint(edge), planner.Midpoint(seeds[i])))].Add(edge);
        }
        return members.Select(m => new DraftSection(m)).ToList();
    }

    private List<int> Seeds(IReadOnlyCollection<int> edges, int sections, SplitTarget target)
    {
        var weights = edges.ToDictionary(e => e, e => Weight(e, target));
        var total = Math.Max(weights.Values.Sum(), 1e-9);
        var centre = new GeoPoint(
            edges.Sum(e => planner.Midpoint(e).Lat * weights[e]) / total,
            edges.Sum(e => planner.Midpoint(e).Lon * weights[e]) / total);
        var seeds = new List<int> { edges.MinBy(e => planner.Distance(planner.Midpoint(e), centre)) };
        while (seeds.Count < sections)
        {
            seeds.Add(edges
                .Where(e => !seeds.Contains(e))
                .MaxBy(e => seeds.Min(s => planner.Distance(planner.Midpoint(e), planner.Midpoint(s))) * (weights[e] + 0.01)));
        }
        return seeds;
    }

    private void Balance(SplitGraph graph, List<DraftSection> sections, SplitTarget target)
    {
        var weights = sections.SelectMany(s => s.Edges).Distinct().ToDictionary(e => e, e => Weight(e, target));
        for (var move = 0; move < MaxBalanceMoves; move++)
        {
            var totals = sections.ToDictionary(s => s, s => s.Edges.Sum(e => weights[e]));
            var centres = sections.ToDictionary(s => s, s => Centre(s.Edges));
            var best = default((int Edge, DraftSection From, DraftSection To, double Gain)?);
            foreach (var from in sections)
            {
                foreach (var edge in from.Edges)
                {
                    var weight = weights[edge];
                    if (weight <= 0)
                    {
                        continue;
                    }
                    foreach (var to in NeighboursOfEdge(graph, edge, from, sections))
                    {
                        var difference = totals[from] - totals[to];
                        if (weight >= difference)
                        {
                            continue;
                        }
                        var midpoint = planner.Midpoint(edge);
                        var compactness = planner.Distance(midpoint, centres[from]) - planner.Distance(midpoint, centres[to]);
                        var gain = Math.Min(weight, difference - weight) + compactness / 1000;
                        if ((best is null || gain > best.Value.Gain) && CanGive(graph, from, edge))
                        {
                            best = (edge, from, to, gain);
                        }
                    }
                }
            }
            if (best is not { } chosen)
            {
                return;
            }
            chosen.From.Edges.Remove(chosen.Edge);
            chosen.To.Edges.Add(chosen.Edge);
        }
    }

    private GeoPoint Centre(IReadOnlyCollection<int> edges) => new(
        edges.Average(e => planner.Midpoint(e).Lat),
        edges.Average(e => planner.Midpoint(e).Lon));
}
