namespace GSBC.ChurchApp.Features.Outreach.Routing;

public enum ReachStatus
{
    Full,
    Short,
    Long,
}

public enum ReachReasonKind
{
    AllDoneNearby,
    Rested,
    AreaEdge,
    BoxedIn,
    FarStreets,
    FirstStreet,
    NextTooLong,
}

public sealed record ReachReason(ReachReasonKind Kind, IReadOnlyList<string> Names, int Count = 0, double Metres = 0);

public sealed record HopChoice(WalkHop Hop, string Street, WalkPlan Plan, int Added);

public sealed record StartChoice(int Node, GeoPoint Point, double Metres, string Street, WalkPlan Plan);

public sealed record WalkReach(ReachStatus Status, IReadOnlyList<ReachReason> Reasons, HopChoice? Hop, StartChoice? Elsewhere, bool Capped = false)
{
    public static WalkReach Full { get; } = new(ReachStatus.Full, [], null, null);
}

public sealed class ReachFinder(WalkPlanner planner)
{
    private const double NearbyMetres = 250;
    private const double AreaEdgeMetres = 300;
    private const double FarMetres = 150;
    private const double BoxedInShare = 0.85;
    private const int ReasonsShown = 2;
    private const int HopTries = 4;
    private const int MinHopGain = 20;
    private const int MinHopPocket = 60;
    private const double LongerAsk = 2;
    private const double GrowsBy = 1.15;
    private const double StartSearchMetres = 4000;
    private const int StartTries = 4;
    private const int PocketsLooked = 24;
    private const double PocketCapFactor = 3;

    private readonly WalkNetwork network = planner.Network;
    private readonly Openness open = planner.Open;
    private readonly RouteSettings settings = planner.Open.Settings;

    private sealed record Pocket(HashSet<int> Pieces, HashSet<int> Nodes, int Letterboxes, double Metres, double Seconds, Dictionary<string, double> MainRoads)
    {
        public double Capacity(WalkBudget budget) => budget.ByTime ? Seconds : Metres;
    }

    public ReachStatus StatusOf(WalkBudget budget, WalkPlan walk)
    {
        double asked = budget.Asked, reached = budget.Reached(walk);
        return walk.IsEmpty || reached < asked * settings.ShortOf ? ReachStatus.Short
            : reached > asked * settings.LongOf ? ReachStatus.Long
            : ReachStatus.Full;
    }

    public WalkReach Assess(int start, WalkBudget budget, WalkPlan walk)
    {
        ReachStatus status = StatusOf(budget, walk);
        if (status == ReachStatus.Full)
            return WalkReach.Full;
        Flat from = network.Node(start);
        if (walk.IsEmpty)
        {
            IReadOnlyList<int> done = RestedNear([from]);
            return new WalkReach(status, [new ReachReason(ReachReasonKind.AllDoneNearby, Names(done), done.Count)], null, FindStart(start, budget, null), Capped: true);
        }

        IReadOnlyList<int> rested = RestedNear(WalkPoints(walk));
        List<ReachReason> reasons = [];
        WalkPlan? further = status == ReachStatus.Short ? Longer(start, budget, walk) : null;
        if (status == ReachStatus.Long || further is not null)
        {
            reasons.Add(further is not null ? new ReachReason(ReachReasonKind.NextTooLong, [], further.Letterboxes)
                : walk.ApproachMetres >= FarMetres ? new ReachReason(ReachReasonKind.FarStreets, [], Metres: walk.ApproachMetres)
                : new ReachReason(ReachReasonKind.FirstStreet, [], Metres: walk.Metres));
            if (rested.Count > 0)
                reasons.Add(new ReachReason(ReachReasonKind.Rested, Names(rested), rested.Count));
            return new WalkReach(status, reasons, null, null);
        }

        Pocket pocket = Flood(walk.Pieces.Where(open.Live), double.MaxValue, budget);
        if (rested.Count > 0)
            reasons.Add(new ReachReason(ReachReasonKind.Rested, Names(rested), rested.Count));
        if (AtAreaEdge(pocket))
            reasons.Add(new ReachReason(ReachReasonKind.AreaEdge, []));
        if (walk.Letterboxes >= pocket.Letterboxes * BoxedInShare)
        {
            List<string> roads = pocket.MainRoads.OrderByDescending(r => r.Value).Select(r => r.Key).Take(2).ToList();
            reasons.Add(new ReachReason(ReachReasonKind.BoxedIn, roads, NonHomesNear(walk)));
        }
        return new WalkReach(status, reasons.Take(ReasonsShown).ToList(), FindHop(start, budget, walk, pocket), FindStart(start, budget, pocket), Capped: true);
    }

    public bool CappedAt(int start, WalkBudget budget, WalkPlan walk) =>
        StatusOf(budget, walk) == ReachStatus.Short && Longer(start, budget, walk) is null;

    public IReadOnlyList<int> RestedNear(IReadOnlyList<Flat> points)
    {
        if (points.Count == 0)
            return [];
        Flat low = new(points.Min(p => p.X) - NearbyMetres, points.Min(p => p.Y) - NearbyMetres);
        Flat high = new(points.Max(p => p.X) + NearbyMetres, points.Max(p => p.Y) + NearbyMetres);
        return network.Graph.Index.StreetsIn(low, high)
            .Where(open.Rested)
            .Where(s => Inside(network.Midpoint(s), low, high))
            .ToList();
    }

    private WalkPlan? Longer(int start, WalkBudget budget, WalkPlan walk)
    {
        WalkPlan further = planner.Plan(start, budget.Times(LongerAsk));
        return budget.Reached(further) >= budget.Reached(walk) * GrowsBy ? further : null;
    }

    private HopChoice? FindHop(int start, WalkBudget budget, WalkPlan walk, Pocket pocket)
    {
        List<(WalkHop Hop, Pocket Landing)> found = [];
        Dictionary<int, int> via = [];
        foreach ((int node, double metres, int reachedBy) in planner.Paths.Outward(pocket.Nodes, settings.MaxHopMetres, piece => !open.Live(piece)))
        {
            via[node] = reachedBy;
            if (found.Count >= HopTries)
                break;
            if (pocket.Nodes.Contains(node))
                continue;
            int target = -1;
            foreach (int piece in network.Adjacent(node))
            {
                if (open.Live(piece) && !pocket.Pieces.Contains(piece) && (target < 0 || open.Open(piece) > open.Open(target)))
                    target = piece;
            }
            if (target < 0 || found.Any(f => f.Landing.Pieces.Contains(target)))
                continue;
            List<int> path = [];
            int back = node;
            while (via.TryGetValue(back, out int piece) && piece >= 0)
            {
                path.Insert(0, piece);
                back = network.Other(piece, back);
            }
            found.Add((new WalkHop(back, path, target, metres), Flood([target], budget.Asked * PocketCapFactor, budget)));
        }
        if (found.Count == 0)
            return null;
        double wanted = Math.Max(MinHopPocket, walk.Letterboxes * (budget.Asked / budget.Reached(walk) - 1));
        WalkHop hop = found.Where(f => f.Landing.Letterboxes >= wanted).Select(f => f.Hop).FirstOrDefault()
            ?? found.MaxBy(f => f.Landing.Letterboxes).Hop;
        WalkPlan with = planner.Plan(start, budget, hop);
        int added = with.Letterboxes - walk.Letterboxes;
        return with.Hop is null || added < MinHopGain ? null : new HopChoice(hop, network.Street(hop.Target).Name, with, added);
    }

    private StartChoice? FindStart(int start, WalkBudget budget, Pocket? current)
    {
        double asked = budget.Asked * settings.ShortOf;
        List<Pocket> seen = current is null ? [] : [current];
        int tries = 0;
        foreach ((int node, double metres, _) in planner.Paths.Outward([start], StartSearchMetres))
        {
            if (seen.Count > PocketsLooked || tries >= StartTries)
                return null;
            int live = -1;
            foreach (int piece in network.Adjacent(node))
            {
                if (open.Live(piece) && !seen.Any(p => p.Pieces.Contains(piece)))
                    live = piece;
            }
            if (live < 0)
                continue;
            Pocket pocket = Flood([live], budget.Asked * PocketCapFactor, budget);
            seen.Add(pocket);
            if (pocket.Capacity(budget) < asked)
                continue;
            tries++;
            WalkPlan walk = planner.Plan(node, budget);
            if (StatusOf(budget, walk) != ReachStatus.Short)
                return new StartChoice(node, network.Graph.Projection.ToGeo(network.Node(node)), metres, network.Street(live).Name, walk);
        }
        return null;
    }

    private Pocket Flood(IEnumerable<int> seeds, double cap, WalkBudget budget)
    {
        HashSet<int> nodes = [];
        HashSet<int> pieces = [];
        Dictionary<string, double> roads = [];
        Queue<int> queue = new();
        double metres = 0, seconds = 0;
        int letterboxes = 0;
        foreach (int seed in seeds)
        {
            if (!pieces.Add(seed))
                continue;
            (letterboxes, metres, seconds) = (letterboxes + open.Open(seed), metres + 2 * network[seed].Length, seconds + open.BothSidesSeconds(seed));
            foreach (int end in new[] { network[seed].U, network[seed].V })
            {
                if (nodes.Add(end))
                    queue.Enqueue(end);
            }
        }
        while (queue.TryDequeue(out int node) && (budget.ByTime ? seconds : metres) < cap)
        {
            foreach (int piece in network.Adjacent(node).ToArray())
            {
                if (!open.Live(piece) || !pieces.Add(piece))
                    continue;
                (letterboxes, metres, seconds) = (letterboxes + open.Open(piece), metres + 2 * network[piece].Length, seconds + open.BothSidesSeconds(piece));
                foreach (int end in new[] { network[piece].U, network[piece].V })
                {
                    if (nodes.Add(end))
                        queue.Enqueue(end);
                }
            }
            foreach ((int across, _, int via) in planner.Paths.Outward([node], settings.MaxConnectorLegMetres, piece => !open.Live(piece)))
            {
                if (via >= 0 && network.Street(via).Kind.IsMainRoad() && network.Street(via).Name.Length > 0)
                    roads[network.Street(via).Name] = roads.GetValueOrDefault(network.Street(via).Name) + network[via].Length;
                if (across != node && nodes.Add(across) && HasLive(across))
                    queue.Enqueue(across);
            }
        }
        return new Pocket(pieces, nodes, letterboxes, metres, seconds, roads);
    }

    private bool HasLive(int node)
    {
        foreach (int piece in network.Adjacent(node))
        {
            if (open.Live(piece))
                return true;
        }
        return false;
    }

    private bool AtAreaEdge(Pocket pocket)
    {
        StreetGraph graph = network.Graph;
        Flat low = graph.Projection.ToFlat(new GeoPoint(graph.Region.Bounds.South, graph.Region.Bounds.West)) + new Flat(AreaEdgeMetres, AreaEdgeMetres);
        Flat high = graph.Projection.ToFlat(new GeoPoint(graph.Region.Bounds.North, graph.Region.Bounds.East)) - new Flat(AreaEdgeMetres, AreaEdgeMetres);
        return pocket.Pieces.Any(p => !Inside(network.Midpoint(p), low, high));
    }

    private int NonHomesNear(WalkPlan walk)
    {
        IReadOnlyList<Flat> points = WalkPoints(walk);
        Flat low = new(points.Min(p => p.X) - NearbyMetres, points.Min(p => p.Y) - NearbyMetres);
        Flat high = new(points.Max(p => p.X) + NearbyMetres, points.Max(p => p.Y) + NearbyMetres);
        int count = 0;
        foreach (int street in network.Graph.Index.StreetsIn(low, high))
        {
            for (int side = 0; side < 2; side++)
            {
                LetterboxRange range = network.Graph.Letterboxes(street, (Side)side);
                for (int b = range.Start; b < range.End; b++)
                    count += open.State(b) == LetterboxState.NotAHome && Inside(network.Graph.LetterboxFlat(b), low, high) ? 1 : 0;
            }
        }
        return count;
    }

    private List<Flat> WalkPoints(WalkPlan walk) => walk.Pieces.SelectMany(p => new[] { network.Node(network[p].U), network.Node(network[p].V) }).ToList();

    private IReadOnlyList<string> Names(IEnumerable<int> pieces) =>
        pieces.Select(p => network.Street(p).Name).Where(n => n.Length > 0).Distinct().Order().ToList();

    private static bool Inside(Flat point, Flat low, Flat high) => point.X >= low.X && point.X <= high.X && point.Y >= low.Y && point.Y <= high.Y;
}
