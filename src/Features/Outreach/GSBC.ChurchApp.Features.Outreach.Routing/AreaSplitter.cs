namespace GSBC.ChurchApp.Features.Outreach.Routing;

public enum SplitTarget
{
    Letterboxes,
    Bundles,
    Minutes,
}

public sealed record AreaSelection(IReadOnlyList<int> Fresh, IReadOnlyList<int> Rested, IReadOnlyList<int> MainRoads, IReadOnlyList<int> Excluded, IReadOnlyList<int> Empty)
{
    public IEnumerable<int> Connectors(bool restedToo) => [.. MainRoads, .. Excluded, .. Empty, .. restedToo ? Rested : []];
}

public sealed record SectionSummary(DraftSection Section, string Name, WalkPlan Plan, TimeSpan Time, double TravelMetres, int Pieces)
{
    public int Letterboxes => Plan.Letterboxes;

    public int Bundles => Plan.Bundles;

    public bool InPieces => Pieces > 1;

    public bool NeedsReview => Plan.NeedsReview || InPieces;
}

public sealed class SplitGraph
{
    private readonly WalkNetwork network;
    private readonly double joinMetres;
    private readonly Dictionary<int, int> group = [];
    private readonly HashSet<int> placed = [];

    public SplitGraph(WalkNetwork network, IEnumerable<int> connectors, IReadOnlyCollection<int> streets, double joinMetres)
    {
        this.network = network;
        this.joinMetres = joinMetres;
        JoinShortLinks(connectors, streets);
    }

    public bool Touch(int a, int b)
    {
        if (a == b)
            return false;
        (int au, int av, int bu, int bv) = (Root(network[a].U), Root(network[a].V), Root(network[b].U), Root(network[b].V));
        return au == bu || au == bv || av == bu || av == bv;
    }

    public bool Reaches(int piece, int node)
    {
        int root = Root(node);
        return Root(network[piece].U) == root || Root(network[piece].V) == root;
    }

    public void Join(IEnumerable<int> connectors)
    {
        foreach (int piece in connectors)
            Union(network[piece].U, network[piece].V);
    }

    public List<List<int>> Components(IReadOnlyCollection<int> pieces)
    {
        Dictionary<int, List<int>> byGroup = [];
        foreach (int piece in pieces)
        {
            foreach (int root in new[] { Root(network[piece].U), Root(network[piece].V) }.Distinct())
            {
                if (!byGroup.TryGetValue(root, out List<int>? list))
                    byGroup[root] = list = [];
                list.Add(piece);
            }
        }
        HashSet<int> seen = [];
        List<List<int>> components = [];
        foreach (int start in pieces)
        {
            if (!seen.Add(start))
                continue;
            List<int> component = [];
            Stack<int> stack = new([start]);
            while (stack.TryPop(out int piece))
            {
                component.Add(piece);
                foreach (int next in byGroup[Root(network[piece].U)].Concat(byGroup[Root(network[piece].V)]))
                {
                    if (seen.Add(next))
                        stack.Push(next);
                }
            }
            components.Add(component);
        }
        return components;
    }

    private void JoinShortLinks(IEnumerable<int> connectors, IReadOnlyCollection<int> streets)
    {
        Dictionary<int, List<int>> linksAt = [];
        foreach (int piece in connectors)
        {
            foreach (int node in new[] { network[piece].U, network[piece].V })
            {
                if (!linksAt.TryGetValue(node, out List<int>? list))
                    linksAt[node] = list = [];
                list.Add(piece);
            }
        }
        HashSet<int> ends = streets.SelectMany(p => new[] { network[p].U, network[p].V }).ToHashSet();
        foreach (int origin in ends.Where(linksAt.ContainsKey))
        {
            Dictionary<int, double> distance = new() { [origin] = 0 };
            PriorityQueue<int, double> queue = new();
            queue.Enqueue(origin, 0);
            while (queue.TryDequeue(out int node, out double reached))
            {
                if (reached > distance[node])
                    continue;
                if (node != origin && ends.Contains(node))
                    Union(origin, node);
                foreach (int piece in linksAt.GetValueOrDefault(node) ?? [])
                {
                    int next = network.Other(piece, node);
                    double nextDistance = reached + network[piece].Length;
                    if (nextDistance <= joinMetres && nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
                    {
                        distance[next] = nextDistance;
                        queue.Enqueue(next, nextDistance);
                    }
                }
            }
        }
    }

    private int Root(int node)
    {
        if (network.IsVirtual(node) && placed.Add(node))
        {
            (int street, double at) = network.VirtualNode(node);
            ref readonly StreetInfo whole = ref network.Graph.Street(street);
            (int corner, double metres) = at <= 0.5 ? (whole.U, at * whole.Length) : (whole.V, (1 - at) * whole.Length);
            if (metres <= joinMetres)
                Union(node, corner);
        }
        return Find(node);
    }

    private int Find(int node)
    {
        int root = node;
        while (group.TryGetValue(root, out int parent) && parent != root)
            root = parent;
        while (group.TryGetValue(node, out int parent) && parent != root)
        {
            group[node] = root;
            node = parent;
        }
        return root;
    }

    private void Union(int a, int b)
    {
        (int ra, int rb) = (Find(a), Find(b));
        if (ra != rb)
            group[ra] = rb;
    }
}

public sealed class AreaSplitter(WalkPlanner planner)
{
    private const int MaxBalanceMoves = 400;
    private const double ScrapShare = 1 / 3.0;
    private const double UnknownTravelFactor = 1.3;

    private readonly WalkNetwork network = planner.Network;
    private readonly Openness open = planner.Open;

    public WalkPlanner Planner => planner;

    public AreaSelection Select(IReadOnlyList<GeoPoint> polygon)
    {
        Flat[] area = polygon.Select(network.Graph.Projection.ToFlat).ToArray();
        Flat low = new(area.Min(p => p.X), area.Min(p => p.Y)), high = new(area.Max(p => p.X), area.Max(p => p.Y));
        List<int> inside = network.Graph.Index.StreetsIn(low, high).Where(s => Geometry.Inside(area, network.Midpoint(s))).Order().ToList();
        List<int> streets = inside.Where(s => network.Street(s).CanDeliver && !open.IsExcludedStreet(s)).ToList();
        List<int> delivering = streets.Where(s => network.Graph.Homes(s) > 0).ToList();
        return new AreaSelection(
            delivering.Where(open.Live).ToList(),
            delivering.Where(s => !open.Live(s)).ToList(),
            inside.Where(s => network.Street(s).Kind.IsMainRoad()).ToList(),
            inside.Where(s => network.Street(s).CanDeliver && open.IsExcludedStreet(s)).ToList(),
            streets.Except(delivering).Concat(inside.Where(s => network.Street(s).Kind.IsPath())).ToList());
    }

    public double Weight(int piece, SplitTarget target) => target switch
    {
        SplitTarget.Minutes => open.BothSidesSeconds(piece) / 60,
        SplitTarget.Bundles => open.Open(piece) / (double)open.Settings.BundleSize,
        _ => open.Open(piece),
    };

    public (List<HashSet<int>> Sections, SplitGraph Graph) Split(IEnumerable<int> pieces, SplitTarget target, double perRoute, IEnumerable<int> connectors)
    {
        HashSet<int> streets = pieces.Where(open.Live).ToHashSet();
        SplitGraph graph = new(network, connectors, streets, open.Settings.SplitJoinMetres);
        double total = streets.Sum(p => Weight(p, target));
        if (total <= 0 || perRoute <= 0)
            return ([], graph);
        LinkNearbyIslands(graph, streets);

        var islands = graph.Components(streets)
            .Select(c => (Pieces: c.ToHashSet(), Weight: c.Sum(p => Weight(p, target))))
            .Where(c => c.Weight > 0)
            .OrderByDescending(c => c.Weight)
            .ThenBy(c => c.Pieces.Min())
            .ToList();
        List<HashSet<int>> sections = [];
        foreach ((HashSet<int> island, double weight) in islands)
            sections.AddRange(Grow(graph, island, Math.Min(Math.Max(1, (int)Math.Round(weight / perRoute)), island.Count), target));
        Balance(graph, sections, target);
        if (MergeScraps(graph, sections, target, perRoute))
            Balance(graph, sections, target);
        return (sections, graph);
    }

    public List<HashSet<int>> SplitInTwo(SplitGraph graph, IReadOnlyCollection<int> section, SplitTarget target)
    {
        List<HashSet<int>> halves = Grow(graph, section, Math.Min(2, section.Count), target);
        Balance(graph, halves, target);
        return halves;
    }

    public void Rebalance(SplitGraph graph, List<HashSet<int>> sections, SplitTarget target) => Balance(graph, sections, target);

    public SectionSummary Summarise(DraftSection section, GeoPoint meeting, SplitGraph graph)
    {
        Flat meetingPoint = network.Graph.Projection.ToFlat(meeting);
        int start = section.Start is { } pinned && section.Pieces.Any(p => network[p].U == pinned || network[p].V == pinned)
            ? pinned
            : planner.NearestEnd(section.Pieces, meetingPoint);
        WalkPlan plan = planner.Circuit(section.Pieces, start);
        int? meetingNode = planner.StartNear(meeting);
        double travel = meetingNode is { } node ? planner.Paths.Metres(node, plan.Start, network.CutsOf(plan.Pieces)) : double.NaN;
        if (double.IsNaN(travel))
            travel = Flat.Distance(meetingPoint, network.Node(plan.Start)) * UnknownTravelFactor;
        TimeSpan time = plan.Time + TimeSpan.FromSeconds(open.WalkSeconds(2 * travel));
        return new SectionSummary(section, section.Name ?? Name(section.Pieces), plan, time, travel, graph.Components(section.Pieces.ToList()).Count);
    }

    public string Name(IEnumerable<int> pieces)
    {
        string name = string.Join(" & ", pieces
            .Where(p => network.Street(p).Name.Length > 0)
            .GroupBy(p => network.Street(p).Name)
            .OrderByDescending(g => g.Sum(open.Open))
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Take(2)
            .Select(g => g.Key));
        return name.Length > 0 ? name : "Unnamed streets";
    }

    public static bool CanGive(SplitGraph graph, IReadOnlyCollection<int> from, int piece) =>
        from.Count > 1 && graph.Components(from.Where(p => p != piece).ToList()).Count <= graph.Components(from).Count;

    private bool MergeScraps(SplitGraph graph, List<HashSet<int>> sections, SplitTarget target, double perRoute)
    {
        double Total(HashSet<int> s) => s.Sum(p => Weight(p, target));
        bool merged = false;
        while (sections.Count > 1 && sections.MinBy(Total) is { } scrap && Total(scrap) < perRoute * ScrapShare)
        {
            if (sections.Where(s => s != scrap && s.Any(p => scrap.Any(q => graph.Touch(p, q)))).MinBy(Total) is not { } into)
                break;
            into.UnionWith(scrap);
            sections.Remove(scrap);
            merged = true;
        }
        return merged;
    }

    private void LinkNearbyIslands(SplitGraph graph, HashSet<int> streets)
    {
        while (graph.Components(streets) is { Count: > 1 } islands)
        {
            List<int>? best = null;
            double bestMetres = double.MaxValue;
            foreach (List<int> island in islands)
            {
                HashSet<int> targets = streets.Except(island).SelectMany(p => new[] { network[p].U, network[p].V }).ToHashSet();
                List<int> path = planner.Paths.Shortest(island.SelectMany(p => new[] { network[p].U, network[p].V }).Distinct(), targets.Contains, open.Settings.SplitJoinMetres, network.CutsOf(streets));
                double metres = path.Sum(p => network[p].Length);
                if (path.Count > 0 && metres < bestMetres)
                    (best, bestMetres) = (path, metres);
            }
            if (best is null)
                return;
            graph.Join(best);
        }
    }

    private List<HashSet<int>> Grow(SplitGraph graph, IReadOnlyCollection<int> pieces, int count, SplitTarget target)
    {
        List<int> seeds = Seeds(pieces, count, target);
        double[] totals = seeds.Select(s => Weight(s, target)).ToArray();
        List<HashSet<int>> members = seeds.Select(s => new HashSet<int> { s }).ToList();
        HashSet<int> growing = Enumerable.Range(0, seeds.Count).ToHashSet();
        HashSet<int> unassigned = pieces.Except(seeds).ToHashSet();
        while (unassigned.Count > 0 && growing.Count > 0)
        {
            int lightest = growing.MinBy(i => totals[i]);
            Flat seedPoint = network.Midpoint(seeds[lightest]);
            int? next = unassigned
                .Where(p => members[lightest].Any(m => graph.Touch(p, m)))
                .OrderBy(p => Flat.Distance(network.Midpoint(p), seedPoint))
                .ThenBy(p => p)
                .Select(p => (int?)p)
                .FirstOrDefault();
            if (next is not { } piece)
            {
                growing.Remove(lightest);
                continue;
            }
            unassigned.Remove(piece);
            members[lightest].Add(piece);
            totals[lightest] += Weight(piece, target);
        }
        foreach (int piece in unassigned)
            members[Enumerable.Range(0, seeds.Count).MinBy(i => Flat.Distance(network.Midpoint(piece), network.Midpoint(seeds[i])))].Add(piece);
        return members;
    }

    private List<int> Seeds(IReadOnlyCollection<int> pieces, int count, SplitTarget target)
    {
        Dictionary<int, double> weights = pieces.ToDictionary(p => p, p => Weight(p, target));
        double total = Math.Max(weights.Values.Sum(), 1e-9);
        Flat centre = new(pieces.Sum(p => network.Midpoint(p).X * weights[p]) / total, pieces.Sum(p => network.Midpoint(p).Y * weights[p]) / total);
        List<int> seeds = [pieces.OrderBy(p => p).MinBy(p => Flat.Distance(network.Midpoint(p), centre))];
        while (seeds.Count < count)
        {
            seeds.Add(pieces
                .Where(p => !seeds.Contains(p))
                .OrderBy(p => p)
                .MaxBy(p => seeds.Min(s => Flat.Distance(network.Midpoint(p), network.Midpoint(s))) * (weights[p] + 0.01)));
        }
        return seeds;
    }

    private void Balance(SplitGraph graph, List<HashSet<int>> sections, SplitTarget target)
    {
        Dictionary<int, double> weights = sections.SelectMany(s => s).Distinct().ToDictionary(p => p, p => Weight(p, target));
        for (int move = 0; move < MaxBalanceMoves; move++)
        {
            double[] totals = sections.Select(s => s.Sum(p => weights[p])).ToArray();
            Flat[] centres = sections.Select(Centre).ToArray();
            (int Piece, int From, int To, double Gain)? best = null;
            for (int from = 0; from < sections.Count; from++)
            {
                foreach (int piece in sections[from].Order())
                {
                    double weight = weights[piece];
                    if (weight <= 0)
                        continue;
                    for (int to = 0; to < sections.Count; to++)
                    {
                        double difference = totals[from] - totals[to];
                        if (to == from || weight >= difference || !sections[to].Any(p => graph.Touch(p, piece)))
                            continue;
                        Flat middle = network.Midpoint(piece);
                        double compactness = Flat.Distance(middle, centres[from]) - Flat.Distance(middle, centres[to]);
                        double gain = Math.Min(weight, difference - weight) + compactness / 1000;
                        if ((best is null || gain > best.Value.Gain) && CanGive(graph, sections[from], piece))
                            best = (piece, from, to, gain);
                    }
                }
            }
            if (best is not { } chosen)
                return;
            sections[chosen.From].Remove(chosen.Piece);
            sections[chosen.To].Add(chosen.Piece);
        }
    }

    private Flat Centre(IReadOnlyCollection<int> pieces) =>
        new(pieces.Average(p => network.Midpoint(p).X), pieces.Average(p => network.Midpoint(p).Y));
}
