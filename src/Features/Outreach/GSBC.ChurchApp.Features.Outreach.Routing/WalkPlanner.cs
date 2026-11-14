namespace GSBC.ChurchApp.Features.Outreach.Routing;

public sealed class WalkPlanner
{
    private const double BudgetSlack = 1.02;
    private const double SeedSpacingMetres = 200;
    private const int SeedTries = 3;
    private const double HopLeadMetres = 400;
    private const double StartSearchMetres = 2000;

    private readonly WalkNetwork network;
    private readonly StreetGraph graph;
    private readonly RouteSettings settings;

    public WalkPlanner(Openness open)
    {
        Open = open;
        network = open.Network;
        graph = network.Graph;
        settings = open.Settings;
        Paths = new Paths(network);
    }

    public Openness Open { get; }

    public Paths Paths { get; }

    public WalkNetwork Network => network;

    public int? StartNear(GeoPoint point) =>
        graph.Index.NearestNode(graph.Projection.ToFlat(point), StartSearchMetres, node => Walkable(node));

    public WalkPlan Plan(int start, WalkBudget budget, WalkHop? hop = null)
    {
        WalkPlan? best = null;
        foreach ((int seed, List<int> approach) in Seeds(start))
        {
            WalkPlan plan = Grow(start, seed, approach, budget, hop);
            if (best is null || plan.Letterboxes > best.Letterboxes || plan.Letterboxes == best.Letterboxes && plan.Metres < best.Metres)
                best = plan;
        }
        return best ?? Circuit([], start);
    }

    public WalkPlan Circuit(IEnumerable<int> pieces, int start, IReadOnlySet<int>? walkOnly = null, IReadOnlySet<int>? approach = null)
    {
        HashSet<int> connectors = walkOnly is null ? [] : [.. walkOnly];
        HashSet<int> set = pieces.Where(p => connectors.Contains(p) || Open.Live(p)).ToHashSet();
        set.UnionWith(connectors);
        if (set.Count > 0 && !set.Any(p => network[p].U == start || network[p].V == start))
            start = NearestEnd(set, network.Node(start));
        Cuts cuts = network.CutsOf(set);
        foreach (int link in Links(set, start, cuts))
        {
            if (set.Add(link))
                connectors.Add(link);
        }

        Dictionary<int, List<Arc>> arcsFrom = [];
        foreach (int piece in set.Order())
        {
            Outgoing(arcsFrom, network[piece].U).Add(new Arc(piece, true));
            Outgoing(arcsFrom, network[piece].V).Add(new Arc(piece, false));
        }
        HashSet<Arc> used = [];

        List<Arc> Tour(int from, Arc? came)
        {
            List<Arc> path = [];
            while (NextArc(from, came, arcsFrom, used) is { } arc)
            {
                used.Add(arc);
                path.Add(arc);
                from = To(arc);
                came = arc;
            }
            return path;
        }

        List<Arc> route = Tour(start, null);
        for (int i = 0; i < route.Count; i++)
            route.InsertRange(i, Tour(From(route[i]), i > 0 ? route[i - 1] : null));
        foreach (Arc arc in arcsFrom.Values.SelectMany(a => a).ToList())
        {
            if (!used.Contains(arc))
                route.AddRange(Tour(From(arc), null));
        }
        return Summarise(start, route, connectors, approach);
    }

    public WalkPlan Summarise(int start, IReadOnlyList<Arc> route, IReadOnlySet<int> connectors, IReadOnlySet<int>? approach = null)
    {
        int Boxes(Arc arc) => connectors.Contains(arc.Piece) ? 0 : Open.Open(arc.Piece, arc.Side);

        int first = 0, last = route.Count - 1;
        while (first < route.Count && Boxes(route[first]) == 0)
            first++;
        while (last >= 0 && Boxes(route[last]) == 0)
            last--;

        double connectorMetres = 0, longest = 0, run = 0, approachMetres = 0, seconds = 0, metres = 0;
        List<WalkLeg> legs = [];
        Arc? previous = null;
        for (int i = 0; i < route.Count; i++)
        {
            Arc arc = route[i];
            Piece piece = network[arc.Piece];
            int boxes = Boxes(arc);
            metres += piece.Length;
            seconds += Open.WalkSeconds(piece.Length) + (boxes > 0 ? Open.DeliverySeconds(arc.Piece, arc.Side) : 0);
            if (i < first || i > last || approach?.Contains(arc.Piece) == true && boxes == 0)
            {
                approachMetres += piece.Length;
            }
            else if (connectors.Contains(arc.Piece) || !Open.Live(arc.Piece))
            {
                run += piece.Length;
                connectorMetres += piece.Length;
                longest = Math.Max(longest, run);
            }
            else
            {
                run = 0;
            }

            string street = network.Street(arc.Piece).Name is { Length: > 0 } name ? name : WalkLeg.Unnamed;
            bool turnedBack = previous is { } before && network[before.Piece].Street == piece.Street && before.Forward != arc.Forward;
            bool part = boxes > 0 && piece.Partial;
            if (legs.Count > 0 && legs[^1].Street == street && legs[^1].Delivers == boxes > 0 && !turnedBack)
            {
                WalkLeg leg = legs[^1];
                legs[^1] = leg with { Arcs = [.. leg.Arcs, arc], Letterboxes = leg.Letterboxes + boxes, Metres = leg.Metres + piece.Length, Part = leg.Part || part };
            }
            else
            {
                legs.Add(new WalkLeg(street, arc.Side, [arc], boxes, piece.Length, part));
            }
            previous = arc;
        }
        int letterboxes = legs.Sum(l => l.Letterboxes);
        int bundles = (letterboxes + settings.BundleSize - 1) / settings.BundleSize;
        int toTake = (int)Math.Ceiling(letterboxes * (1 + settings.SpareShare) / settings.BundleSize);
        bool review = connectorMetres > settings.MaxConnectorTotalMetres || longest > settings.MaxConnectorLegMetres;
        return new WalkPlan(start, route, legs, metres, letterboxes, TimeSpan.FromSeconds(seconds), connectorMetres, longest, approachMetres, bundles, toTake, review);
    }

    public double Cost(int piece, WalkBudget budget) => budget.ByTime ? Open.BothSidesSeconds(piece) : 2 * network[piece].Length;

    public double TransitCost(int piece, WalkBudget budget) =>
        budget.ByTime ? Open.WalkSeconds(2 * network[piece].Length) : 2 * network[piece].Length;

    private IEnumerable<(int Seed, List<int> Approach)> Seeds(int start)
    {
        Dictionary<int, int> via = [];
        HashSet<int> seen = [];
        List<Flat> seeds = [];
        foreach ((int node, _, int reachedBy) in Paths.Outward([start], settings.MaxApproachMetres))
        {
            via[node] = reachedBy;
            foreach (int piece in network.Adjacent(node).ToArray())
            {
                if (!seen.Add(piece) || !Open.Live(piece))
                    continue;
                Flat middle = network.Midpoint(piece);
                if (seeds.Any(s => Flat.Distance(s, middle) < SeedSpacingMetres))
                    continue;
                seeds.Add(middle);
                yield return (piece, PathBack(node, via));
                if (seeds.Count >= SeedTries)
                    yield break;
            }
        }
    }

    private List<int> PathBack(int node, Dictionary<int, int> via)
    {
        List<int> path = [];
        while (via.TryGetValue(node, out int piece) && piece >= 0)
        {
            path.Add(piece);
            node = network.Other(piece, node);
        }
        path.Reverse();
        return path;
    }

    private WalkPlan Grow(int start, int seed, List<int> approach, WalkBudget budget, WalkHop? hop)
    {
        double limit = budget.Asked;
        HashSet<int> chosen = [.. approach, seed];
        HashSet<int> transit = approach.Where(p => !Open.Live(p)).ToHashSet();
        HashSet<int> walkTo = [.. transit];
        HashSet<int> nodes = [];
        double total = 0;
        foreach (int piece in chosen)
        {
            total += transit.Contains(piece) ? TransitCost(piece, budget) : Cost(piece, budget);
            nodes.Add(network[piece].U);
            nodes.Add(network[piece].V);
        }
        Flat centre = network.Midpoint(seed);
        PriorityQueue<int, double> queue = new();
        Dictionary<int, (List<int> Path, double Metres)> pathTo = [];

        void Discover(IEnumerable<int> from)
        {
            Dictionary<int, int> via = [];
            foreach ((int node, double metres, int reachedBy) in Paths.Outward(from, settings.MaxConnectorLegMetres, piece => !Open.Live(piece)))
            {
                via[node] = reachedBy;
                foreach (int piece in network.Adjacent(node))
                {
                    if (chosen.Contains(piece) || !Open.Live(piece))
                        continue;
                    if (pathTo.TryGetValue(piece, out var known) && known.Metres <= metres)
                        continue;
                    pathTo[piece] = (PathBack(node, via), metres);
                    queue.Enqueue(piece, Flat.Distance(centre, network.Midpoint(piece)) + 2 * metres);
                }
            }
        }

        void Add(int piece, IEnumerable<int> path)
        {
            List<int> fresh = [];
            foreach (int step in path.Append(piece))
            {
                if (!chosen.Add(step))
                    continue;
                fresh.Add(step);
                if (step != piece && !Open.Live(step))
                    transit.Add(step);
            }
            List<int> ends = fresh.SelectMany(p => new[] { network[p].U, network[p].V }).Where(nodes.Add).ToList();
            Discover(ends);
        }

        double CostOf(int piece, IEnumerable<int> path) =>
            Cost(piece, budget) + path.Where(p => !chosen.Contains(p)).Sum(p => Open.Live(p) ? Cost(p, budget) : TransitCost(p, budget));

        void Fill()
        {
            while (total < limit && queue.TryDequeue(out int piece, out _))
            {
                if (chosen.Contains(piece))
                    continue;
                List<int> path = pathTo[piece].Path;
                double cost = CostOf(piece, path);
                if (total + cost > limit * BudgetSlack)
                    continue;
                total += cost;
                Add(piece, path);
            }
        }

        Discover(nodes.ToList());
        Fill();
        if (hop is null || total >= limit)
            return Circuit(chosen, start, transit, walkTo);
        List<int> lead = nodes.Contains(hop.From) ? [] : Paths.Shortest(nodes, n => n == hop.From, HopLeadMetres);
        if (!nodes.Contains(hop.From) && lead.Count == 0)
            return Circuit(chosen, start, transit, walkTo);
        List<int> jump = [.. lead, .. hop.Pieces];
        double hopCost = CostOf(hop.Target, jump);
        if (total + hopCost > limit * BudgetSlack)
            return Circuit(chosen, start, transit, walkTo);
        total += hopCost;
        foreach (int step in jump.Where(p => !Open.Live(p)))
            transit.Add(step);
        Add(hop.Target, jump);
        Fill();
        return Circuit(chosen, start, transit, walkTo) with { Hop = hop };
    }

    private IEnumerable<int> Links(HashSet<int> pieces, int start, Cuts cuts)
    {
        HashSet<int> reached = [start];
        HashSet<int> pending = [.. pieces];
        List<int> links = [];
        while (true)
        {
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (int piece in pending.Where(p => reached.Contains(network[p].U) || reached.Contains(network[p].V)).ToList())
                {
                    pending.Remove(piece);
                    reached.Add(network[piece].U);
                    reached.Add(network[piece].V);
                    grew = true;
                }
            }
            if (pending.Count == 0)
                return links;
            HashSet<int> targets = pending.SelectMany(p => new[] { network[p].U, network[p].V }).ToHashSet();
            List<int> path = Paths.Shortest(reached, targets.Contains, cuts: cuts);
            if (path.Count == 0)
                return links;
            links.AddRange(path);
            foreach (int piece in path)
            {
                reached.Add(network[piece].U);
                reached.Add(network[piece].V);
            }
        }
    }

    public int NearestEnd(IEnumerable<int> pieces, Flat point) =>
        pieces.SelectMany(p => new[] { network[p].U, network[p].V }).MinBy(n => Flat.Distance(network.Node(n), point));

    private Arc? NextArc(int node, Arc? came, Dictionary<int, List<Arc>> arcsFrom, HashSet<Arc> used)
    {
        if (!arcsFrom.TryGetValue(node, out List<Arc>? options))
            return null;
        Arc? best = null;
        double bestTurn = double.MinValue;
        foreach (Arc arc in options)
        {
            if (used.Contains(arc))
                continue;
            if (came is not { } incoming)
                return arc;
            double turn = arc.Piece == incoming.Piece
                ? -Math.PI
                : Geometry.AngleBetween(network.InBearing(incoming.Piece, incoming.Forward), network.OutBearing(arc.Piece, arc.Forward));
            if (turn > bestTurn)
                (best, bestTurn) = (arc, turn);
        }
        return best;
    }

    private bool Walkable(int node)
    {
        foreach (int street in graph.StreetsAt(node))
        {
            if (!graph.Street(street).Kind.IsMainRoad())
                return true;
        }
        return false;
    }

    private int From(Arc arc) => arc.Forward ? network[arc.Piece].U : network[arc.Piece].V;

    private int To(Arc arc) => arc.Forward ? network[arc.Piece].V : network[arc.Piece].U;

    private static List<Arc> Outgoing(Dictionary<int, List<Arc>> arcsFrom, int node)
    {
        if (!arcsFrom.TryGetValue(node, out List<Arc>? list))
            arcsFrom[node] = list = [];
        return list;
    }
}
