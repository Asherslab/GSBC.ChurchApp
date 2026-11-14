namespace GSBC.ChurchApp.Features.Outreach.Routing;

public readonly record struct Piece(int Id, int Street, double From, double To, int U, int V, double Length)
{
    public bool Partial => From > 0 || To < 1;

    public Stretch Stretch => new(Street, From, To);
}

public sealed class WalkNetwork
{
    private const int Steps = 10_000;

    private readonly List<Piece> pieces = [];
    private readonly Dictionary<(int Street, int From, int To), int> pieceIds = [];
    private readonly Dictionary<(int Street, int At), int> nodeIds = [];
    private readonly List<(int Street, double At)> virtualNodes = [];

    public WalkNetwork(StreetGraph graph)
    {
        Graph = graph;
        for (int s = 0; s < graph.StreetCount; s++)
        {
            ref readonly StreetInfo street = ref graph.Street(s);
            pieces.Add(new Piece(s, s, 0, 1, street.U, street.V, street.Length));
        }
    }

    public StreetGraph Graph { get; }

    public Piece this[int piece] => pieces[piece];

    public int PieceCount => pieces.Count;

    public int NodeCount => Graph.NodeCount + virtualNodes.Count;

    public bool IsVirtual(int node) => node >= Graph.NodeCount;

    public (int Street, double At) VirtualNode(int node) => virtualNodes[node - Graph.NodeCount];

    public static double Round(double fraction) => Math.Round(Math.Clamp(fraction, 0, 1) * Steps) / Steps;

    public int PieceOf(Stretch stretch) => PieceOf(stretch.Street, stretch.From, stretch.To);

    public int PieceOf(int street, double from, double to)
    {
        int low = (int)Math.Round(Math.Clamp(Math.Min(from, to), 0, 1) * Steps);
        int high = (int)Math.Round(Math.Clamp(Math.Max(from, to), 0, 1) * Steps);
        if (low == 0 && high == Steps)
            return street;
        if (!pieceIds.TryGetValue((street, low, high), out int id))
        {
            id = pieces.Count;
            pieces.Add(new Piece(id, street, low / (double)Steps, high / (double)Steps, NodeAt(street, low / (double)Steps), NodeAt(street, high / (double)Steps),
                Graph.Street(street).Length * (high - low) / Steps));
            pieceIds[(street, low, high)] = id;
        }
        return id;
    }

    public int NodeAt(int street, double at)
    {
        int step = (int)Math.Round(Math.Clamp(at, 0, 1) * Steps);
        if (step <= 0)
            return Graph.Street(street).U;
        if (step >= Steps)
            return Graph.Street(street).V;
        if (!nodeIds.TryGetValue((street, step), out int node))
        {
            node = Graph.NodeCount + virtualNodes.Count;
            virtualNodes.Add((street, step / (double)Steps));
            nodeIds[(street, step)] = node;
        }
        return node;
    }

    public Flat Node(int node)
    {
        if (!IsVirtual(node))
            return Graph.NodeFlat(node);
        (int street, double at) = VirtualNode(node);
        return Graph.PointAt(street, at);
    }

    public int Other(int piece, int node) => pieces[piece].U == node ? pieces[piece].V : pieces[piece].U;

    public ReadOnlySpan<int> Adjacent(int node, Cuts? cuts = null)
    {
        if (IsVirtual(node))
        {
            (int street, double at) = VirtualNode(node);
            double[] around = cuts?.Of(street) ?? [];
            double before = 0, after = 1;
            foreach (double cut in around)
            {
                if (cut < at - 1e-9)
                    before = cut;
                else if (cut > at + 1e-9 && after >= 1)
                    after = cut;
            }
            return new[] { PieceOf(street, before, at), PieceOf(street, at, after) };
        }
        ReadOnlySpan<int> streets = Graph.StreetsAt(node);
        if (cuts is null || cuts.IsEmpty)
            return streets;
        int[] adjusted = new int[streets.Length * 2];
        int count = 0;
        foreach (int street in streets)
        {
            if (cuts.Of(street) is not { Length: > 0 } cut)
            {
                adjusted[count++] = street;
                continue;
            }
            if (Graph.Street(street).U == node)
                adjusted[count++] = PieceOf(street, 0, cut[0]);
            if (Graph.Street(street).V == node)
                adjusted[count++] = PieceOf(street, cut[^1], 1);
        }
        return adjusted.AsSpan(0, count);
    }

    public Cuts CutsOf(IEnumerable<int> chosen)
    {
        Dictionary<int, SortedSet<double>> cuts = [];
        foreach (int id in chosen)
        {
            Piece piece = pieces[id];
            if (!piece.Partial)
                continue;
            if (!cuts.TryGetValue(piece.Street, out SortedSet<double>? set))
                cuts[piece.Street] = set = [];
            if (piece.From > 0)
                set.Add(piece.From);
            if (piece.To < 1)
                set.Add(piece.To);
        }
        return new Cuts(cuts.ToDictionary(c => c.Key, c => c.Value.ToArray()));
    }

    public IEnumerable<int> Split(int street, IReadOnlyList<double> cuts)
    {
        double at = 0;
        foreach (double cut in cuts)
        {
            yield return PieceOf(street, at, cut);
            at = cut;
        }
        yield return PieceOf(street, at, 1);
    }

    public Flat Midpoint(int piece) => Graph.PointAt(pieces[piece].Street, (pieces[piece].From + pieces[piece].To) / 2);

    public IReadOnlyList<GeoPoint> Line(int piece, bool forward = true)
    {
        Piece p = pieces[piece];
        return forward ? Graph.ShapePoints(p.Street, p.From, p.To) : Graph.ShapePoints(p.Street, p.To, p.From);
    }

    public double OutBearing(int piece, bool forward)
    {
        Piece p = pieces[piece];
        if (!p.Partial)
            return Graph.StartBearing(p.Street, forward);
        double heading = Graph.HeadingAt(p.Street, forward ? p.From + 1e-6 : p.To - 1e-6);
        return forward ? heading : heading + Math.PI;
    }

    public double InBearing(int piece, bool forward)
    {
        Piece p = pieces[piece];
        if (!p.Partial)
            return Graph.EndBearing(p.Street, forward);
        double heading = Graph.HeadingAt(p.Street, forward ? p.To - 1e-6 : p.From + 1e-6);
        return forward ? heading : heading + Math.PI;
    }

    public LetterboxRange Letterboxes(int piece, Side side)
    {
        Piece p = pieces[piece];
        return p.Partial ? Graph.Between(p.Street, side, p.From, p.To) : Graph.Letterboxes(p.Street, side);
    }

    public ref readonly StreetInfo Street(int piece) => ref Graph.Street(pieces[piece].Street);
}

public sealed class Cuts(Dictionary<int, double[]> cuts)
{
    public static Cuts None { get; } = new([]);

    public bool IsEmpty => cuts.Count == 0;

    public double[]? Of(int street) => cuts.GetValueOrDefault(street);
}
