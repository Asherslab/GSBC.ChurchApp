namespace Gsbc.AppPoc.Data.Outreach;

public readonly record struct Stretch(int Street, double From, double To);

public sealed class Piece(StreetNetwork network, int id, int street, double from, double to, int u, int v)
{
    private double[][]? line;

    public int Id { get; } = id;

    public int Street { get; } = street;

    public double From { get; } = from;

    public double To { get; } = to;

    public int U { get; } = u;

    public int V { get; } = v;

    public bool Partial => From > 0 || To < 1;

    public StreetEdge Whole => network.Map.Edges[Street];

    public string Name => Whole.Name;

    public string Road => Whole.Road;

    public bool IsArterial => Whole.IsArterial;

    public int Length => Partial ? Math.Max(1, (int)Math.Round(Whole.Length * (To - From))) : Whole.Length;

    public double[][] Line => line ??= Partial ? network.Slice(Street, From, To) : Whole.Line;

    public int Boxes(int side) => Partial ? network.Within(Street, side, From, To) : Whole.Boxes[side];

    public int Units(int side) => Partial ? Share(Whole.Units[side]) : Whole.Units[side];

    public bool Covers(double at) => StreetNetwork.InRange(From, To, at);

    private int Share(int total) => (int)Math.Round(total * To) - (int)Math.Round(total * From);
}

public sealed class StreetNetwork
{
    private const double Steps = 10000;

    private readonly List<Piece> pieces = [];
    private readonly Dictionary<(int Street, int From, int To), int> pieceIds = [];
    private readonly Dictionary<(int Street, int At), int> nodeIds = [];
    private readonly List<(int Street, double At)> virtualNodes = [];
    private readonly GeoPoint[] corners;
    private readonly List<int>[] adjacency;
    private readonly double[][] along;
    private readonly double cosLat;

    public StreetNetwork(OutreachMap map)
    {
        Map = map;
        BaseNodes = map.Edges.Max(e => Math.Max(e.U, e.V)) + 1;
        corners = new GeoPoint[BaseNodes];
        adjacency = Enumerable.Range(0, BaseNodes).Select(_ => new List<int>()).ToArray();
        cosLat = Math.Cos((map.Bounds[0] + map.Bounds[2]) / 2 * Math.PI / 180);
        along = new double[map.Edges.Count][];
        for (var i = 0; i < map.Edges.Count; i++)
        {
            var edge = map.Edges[i];
            corners[edge.U] = new(edge.Line[0][1], edge.Line[0][0]);
            corners[edge.V] = new(edge.Line[^1][1], edge.Line[^1][0]);
            adjacency[edge.U].Add(i);
            adjacency[edge.V].Add(i);
            pieces.Add(new(this, i, i, 0, 1, edge.U, edge.V));
        }
    }

    public OutreachMap Map { get; }

    public int BaseNodes { get; }

    public double CosLat => cosLat;

    public Piece this[int id] => pieces[id];

    public static bool InRange(double from, double to, double at) => at >= from && (at < to || to >= 1);

    public static double Round(double fraction) => Math.Round(Math.Clamp(fraction, 0, 1) * Steps) / Steps;

    public int PieceOf(int street, double from, double to)
    {
        var key = (street, (int)Math.Round(from * Steps), (int)Math.Round(to * Steps));
        if (key.Item2 == 0 && key.Item3 == (int)Steps)
        {
            return street;
        }
        if (!pieceIds.TryGetValue(key, out var id))
        {
            id = pieces.Count;
            pieces.Add(new(this, id, street, key.Item2 / Steps, key.Item3 / Steps, NodeAt(street, key.Item2 / Steps), NodeAt(street, key.Item3 / Steps)));
            pieceIds[key] = id;
        }
        return id;
    }

    public int NodeAt(int street, double at)
    {
        var step = (int)Math.Round(at * Steps);
        if (step <= 0)
        {
            return Map.Edges[street].U;
        }
        if (step >= Steps)
        {
            return Map.Edges[street].V;
        }
        if (!nodeIds.TryGetValue((street, step), out var node))
        {
            node = BaseNodes + virtualNodes.Count;
            virtualNodes.Add((street, step / Steps));
            nodeIds[(street, step)] = node;
        }
        return node;
    }

    public bool IsVirtual(int node) => node >= BaseNodes;

    public (int Street, double At) VirtualNode(int node) => virtualNodes[node - BaseNodes];

    public GeoPoint Node(int node)
    {
        if (!IsVirtual(node))
        {
            return corners[node];
        }
        var (street, at) = VirtualNode(node);
        var point = Slice(street, at, at)[0];
        return new(point[1], point[0]);
    }

    public IEnumerable<int> CornerNodes => Enumerable.Range(0, BaseNodes);

    public IEnumerable<int> Adjacent(int node, IReadOnlyDictionary<int, double[]>? cuts = null)
    {
        if (IsVirtual(node))
        {
            var (street, at) = VirtualNode(node);
            var around = (cuts?.GetValueOrDefault(street) ?? []).Append(at).Distinct().Order().ToArray();
            var i = Array.IndexOf(around, at);
            yield return PieceOf(street, i > 0 ? around[i - 1] : 0, at);
            yield return PieceOf(street, at, i < around.Length - 1 ? around[i + 1] : 1);
            yield break;
        }
        foreach (var e in adjacency[node])
        {
            if (cuts?.GetValueOrDefault(e) is not { Length: > 0 } cut)
            {
                yield return e;
                continue;
            }
            if (Map.Edges[e].U == node)
            {
                yield return PieceOf(e, 0, cut[0]);
            }
            if (Map.Edges[e].V == node)
            {
                yield return PieceOf(e, cut[^1], 1);
            }
        }
    }

    public Dictionary<int, double[]> CutsOf(IEnumerable<int> edges)
    {
        var cuts = new Dictionary<int, SortedSet<double>>();
        foreach (var piece in edges.Select(e => pieces[e]).Where(p => p.Partial))
        {
            if (!cuts.TryGetValue(piece.Street, out var set))
            {
                cuts[piece.Street] = set = [];
            }
            foreach (var at in new[] { piece.From, piece.To }.Where(at => at is > 0 and < 1))
            {
                set.Add(at);
            }
        }
        return cuts.ToDictionary(c => c.Key, c => c.Value.ToArray());
    }

    public IEnumerable<int> Split(int street, IReadOnlyList<double> cuts)
    {
        var at = 0.0;
        foreach (var cut in cuts)
        {
            yield return PieceOf(street, at, cut);
            at = cut;
        }
        yield return PieceOf(street, at, 1);
    }

    public double Along(int street, GeoPoint point)
    {
        var line = Map.Edges[street].Line;
        var lengths = Cumulative(street);
        var (px, py) = (point.Lon * cosLat, point.Lat);
        var best = (Distance: double.MaxValue, At: 0.0);
        for (var i = 0; i < line.Length - 1; i++)
        {
            var (ax, ay, bx, by) = (line[i][0] * cosLat, line[i][1], line[i + 1][0] * cosLat, line[i + 1][1]);
            var (dx, dy) = (bx - ax, by - ay);
            var t = Math.Clamp(((px - ax) * dx + (py - ay) * dy) / Math.Max(dx * dx + dy * dy, 1e-18), 0, 1);
            var distance = Math.Pow(px - (ax + t * dx), 2) + Math.Pow(py - (ay + t * dy), 2);
            if (distance < best.Distance)
            {
                best = (distance, lengths[i] + t * (lengths[i + 1] - lengths[i]));
            }
        }
        return best.At / Math.Max(lengths[^1], 1e-12);
    }

    public bool Covers(int piece, GeoPoint point) => !pieces[piece].Partial || pieces[piece].Covers(Along(pieces[piece].Street, point));

    public int Within(int street, int side, double from, double to)
    {
        var edge = Map.Edges[street];
        if (edge.At is not { } at)
        {
            return (int)Math.Round(edge.Boxes[side] * to) - (int)Math.Round(edge.Boxes[side] * from);
        }
        return at[side].Count(p => InRange(from, to, p / 1000.0));
    }

    public double[][] Slice(int street, double from, double to)
    {
        var line = Map.Edges[street].Line;
        var lengths = Cumulative(street);
        var (start, end) = (from * lengths[^1], to * lengths[^1]);
        var points = new List<double[]> { PointAt(line, lengths, start) };
        for (var i = 1; i < line.Length - 1; i++)
        {
            if (lengths[i] > start && lengths[i] < end)
            {
                points.Add(line[i]);
            }
        }
        points.Add(PointAt(line, lengths, end));
        return [.. points];
    }

    private static double[] PointAt(double[][] line, double[] lengths, double distance)
    {
        for (var i = 0; i < line.Length - 1; i++)
        {
            if (distance <= lengths[i + 1] || i == line.Length - 2)
            {
                var span = lengths[i + 1] - lengths[i];
                var t = span <= 0 ? 0 : Math.Clamp((distance - lengths[i]) / span, 0, 1);
                return [line[i][0] + t * (line[i + 1][0] - line[i][0]), line[i][1] + t * (line[i + 1][1] - line[i][1])];
            }
        }
        return line[^1];
    }

    private double[] Cumulative(int street)
    {
        if (along[street] is { } cached)
        {
            return cached;
        }
        var line = Map.Edges[street].Line;
        var lengths = new double[line.Length];
        for (var i = 1; i < line.Length; i++)
        {
            var (dx, dy) = ((line[i][0] - line[i - 1][0]) * cosLat, line[i][1] - line[i - 1][1]);
            lengths[i] = lengths[i - 1] + Math.Sqrt(dx * dx + dy * dy);
        }
        return along[street] = lengths;
    }
}
