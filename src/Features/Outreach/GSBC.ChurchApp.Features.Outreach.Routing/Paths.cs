namespace GSBC.ChurchApp.Features.Outreach.Routing;

public sealed class Paths(WalkNetwork network)
{
    public delegate bool PieceFilter(int piece);

    public List<int> Shortest(IEnumerable<int> from, Func<int, bool> isTarget, double maxMetres = double.MaxValue, Cuts? cuts = null, PieceFilter? through = null)
    {
        Dictionary<int, double> distance = [];
        Dictionary<int, int> via = [];
        PriorityQueue<int, double> queue = new();
        foreach (int node in from)
        {
            distance[node] = 0;
            queue.Enqueue(node, 0);
        }
        while (queue.TryDequeue(out int node, out double reached))
        {
            if (reached > distance[node])
                continue;
            if (isTarget(node))
                return Back(node, via);
            foreach (int piece in network.Adjacent(node, cuts))
            {
                if (through is not null && !through(piece))
                    continue;
                int next = network.Other(piece, node);
                double nextDistance = reached + network[piece].Length;
                if (nextDistance <= maxMetres && nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
                {
                    distance[next] = nextDistance;
                    via[next] = piece;
                    queue.Enqueue(next, nextDistance);
                }
            }
        }
        return [];
    }

    public double Metres(int from, int to, Cuts? cuts = null)
    {
        if (from == to)
            return 0;
        List<int> path = Shortest([from], node => node == to, cuts: cuts);
        return path.Count == 0 ? double.NaN : path.Sum(p => network[p].Length);
    }

    public IEnumerable<(int Node, double Metres, int Via)> Outward(IEnumerable<int> from, double maxMetres, PieceFilter? through = null, Cuts? cuts = null)
    {
        Dictionary<int, double> distance = [];
        PriorityQueue<(int Node, int Via), double> queue = new();
        foreach (int node in from)
        {
            distance[node] = 0;
            queue.Enqueue((node, -1), 0);
        }
        while (queue.TryDequeue(out (int Node, int Via) item, out double reached))
        {
            if (reached > distance[item.Node])
                continue;
            yield return (item.Node, reached, item.Via);
            foreach (int piece in network.Adjacent(item.Node, cuts))
            {
                if (through is not null && !through(piece))
                    continue;
                int next = network.Other(piece, item.Node);
                double nextDistance = reached + network[piece].Length;
                if (nextDistance <= maxMetres && nextDistance < distance.GetValueOrDefault(next, double.MaxValue))
                {
                    distance[next] = nextDistance;
                    queue.Enqueue((next, piece), nextDistance);
                }
            }
        }
    }

    private List<int> Back(int node, Dictionary<int, int> via)
    {
        List<int> path = [];
        while (via.TryGetValue(node, out int piece))
        {
            path.Add(piece);
            node = network.Other(piece, node);
        }
        path.Reverse();
        return path;
    }
}
