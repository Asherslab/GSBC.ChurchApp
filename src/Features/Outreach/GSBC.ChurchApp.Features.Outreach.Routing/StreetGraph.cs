namespace GSBC.ChurchApp.Features.Outreach.Routing;

public sealed record StreetRegion(
    string Id,
    string Name,
    long Version,
    DateTimeOffset Built,
    GeoBounds Bounds,
    GeoBounds Core,
    GeoPoint Origin,
    string Attribution);

public readonly record struct StreetInfo(int U, int V, string Name, string FullName, RoadKind Kind, long Way, double Length)
{
    public bool CanDeliver => Kind.CanDeliver();
}

public readonly record struct Letterbox(int Street, Side Side, double Along, LetterboxKind Kind, string? Label, uint AddressKey);

public readonly record struct StreetPosition(int Street, double Along, Side Side, double Offset, double Distance);

public sealed class StreetGraph
{
    private readonly double[] nodeX;
    private readonly double[] nodeY;
    private readonly long[] nodeOsm;
    private readonly StreetInfo[] streets;
    private readonly int[] shapeStart;
    private readonly Flat[] shape;
    private readonly double[] shapeAlong;
    private readonly int[] boxStart;
    private readonly double[] boxAlong;
    private readonly LetterboxKind[] boxKind;
    private readonly string?[] boxLabel;
    private readonly uint[] boxKey;
    private readonly int[] homes;
    private readonly int[] units;
    private readonly int[] adjacentStart;
    private readonly int[] adjacent;
    private Dictionary<StreetKey, int>? byKey;
    private StreetIndex? index;

    internal StreetGraph(
        StreetRegion region,
        double[] nodeX,
        double[] nodeY,
        long[] nodeOsm,
        StreetInfo[] streets,
        int[] shapeStart,
        Flat[] shape,
        int[] boxStart,
        double[] boxAlong,
        LetterboxKind[] boxKind,
        string?[] boxLabel,
        uint[] boxKey)
    {
        Region = region;
        Projection = new LocalProjection(region.Origin);
        this.nodeX = nodeX;
        this.nodeY = nodeY;
        this.nodeOsm = nodeOsm;
        this.streets = streets;
        this.shapeStart = shapeStart;
        this.shape = shape;
        this.boxStart = boxStart;
        this.boxAlong = boxAlong;
        this.boxKind = boxKind;
        this.boxLabel = boxLabel;
        this.boxKey = boxKey;
        shapeAlong = new double[shape.Length];
        for (int s = 0; s < streets.Length; s++)
        {
            int from = shapeStart[s], to = shapeStart[s + 1];
            for (int i = from + 1; i < to; i++)
                shapeAlong[i] = shapeAlong[i - 1] + Flat.Distance(shape[i - 1], shape[i]);
        }
        homes = new int[streets.Length * 2];
        units = new int[streets.Length * 2];
        for (int side = 0; side < homes.Length; side++)
        {
            for (int b = boxStart[side]; b < boxStart[side + 1]; b++)
            {
                homes[side] += boxKind[b].IsHome() ? 1 : 0;
                units[side] += boxKind[b] == LetterboxKind.Unit ? 1 : 0;
            }
        }
        (adjacentStart, adjacent) = Adjacency(nodeX.Length, streets);
    }

    public StreetRegion Region { get; }

    public LocalProjection Projection { get; }

    public int NodeCount => nodeX.Length;

    public int StreetCount => streets.Length;

    public int LetterboxCount => boxAlong.Length;

    public int ShapePointCount => shape.Length;

    public Flat NodeFlat(int node) => new(nodeX[node], nodeY[node]);

    public GeoPoint NodePoint(int node) => Projection.ToGeo(NodeFlat(node));

    public long NodeOsmId(int node) => nodeOsm[node];

    public ref readonly StreetInfo Street(int street) => ref streets[street];

    public StreetKey KeyOf(int street) => new(streets[street].Way, nodeOsm[streets[street].U], nodeOsm[streets[street].V]);

    public int? StreetOf(StreetKey key)
    {
        byKey ??= Enumerable.Range(0, streets.Length).GroupBy(KeyOf).ToDictionary(g => g.Key, g => g.First());
        return byKey.TryGetValue(key, out int street) ? street : null;
    }

    public ReadOnlySpan<int> StreetsAt(int node) => adjacent.AsSpan(adjacentStart[node], adjacentStart[node + 1] - adjacentStart[node]);

    public int OtherEnd(int street, int node) => streets[street].U == node ? streets[street].V : streets[street].U;

    public ReadOnlySpan<Flat> Shape(int street) => shape.AsSpan(shapeStart[street], shapeStart[street + 1] - shapeStart[street]);

    public IReadOnlyList<GeoPoint> ShapePoints(int street, double from = 0, double to = 1)
    {
        List<GeoPoint> points = [Projection.ToGeo(PointAt(street, from))];
        double length = streets[street].Length;
        for (int i = shapeStart[street] + 1; i < shapeStart[street + 1] - 1; i++)
        {
            double at = length <= 0 ? 0 : shapeAlong[i] / length;
            if (at > Math.Min(from, to) && at < Math.Max(from, to))
                points.Add(Projection.ToGeo(shape[i]));
        }
        points.Add(Projection.ToGeo(PointAt(street, to)));
        if (from > to)
            points.Reverse(1, points.Count - 2);
        return points;
    }

    public Flat PointAt(int street, double along)
    {
        int from = shapeStart[street], to = shapeStart[street + 1];
        double target = Math.Clamp(along, 0, 1) * streets[street].Length;
        for (int i = from; i < to - 1; i++)
        {
            if (target <= shapeAlong[i + 1] || i == to - 2)
            {
                double span = shapeAlong[i + 1] - shapeAlong[i];
                double t = span <= 0 ? 0 : Math.Clamp((target - shapeAlong[i]) / span, 0, 1);
                return shape[i] + (shape[i + 1] - shape[i]) * t;
            }
        }
        return shape[from];
    }

    public double HeadingAt(int street, double along)
    {
        int from = shapeStart[street], to = shapeStart[street + 1];
        double target = Math.Clamp(along, 0, 1) * streets[street].Length;
        for (int i = from; i < to - 1; i++)
        {
            if (target <= shapeAlong[i + 1] || i == to - 2)
                return (shape[i + 1] - shape[i]).Bearing;
        }
        return 0;
    }

    public double StartBearing(int street, bool forward)
    {
        ReadOnlySpan<Flat> line = Shape(street);
        return forward ? (line[1] - line[0]).Bearing : (line[^2] - line[^1]).Bearing;
    }

    public double EndBearing(int street, bool forward)
    {
        ReadOnlySpan<Flat> line = Shape(street);
        return forward ? (line[^1] - line[^2]).Bearing : (line[0] - line[1]).Bearing;
    }

    public StreetPosition Project(int street, Flat point)
    {
        int from = shapeStart[street], to = shapeStart[street + 1];
        double best = double.MaxValue, along = 0, offset = 0;
        for (int i = from; i < to - 1; i++)
        {
            Flat a = shape[i], d = shape[i + 1] - a;
            double lengthSquared = Math.Max(Flat.Dot(d, d), 1e-9);
            double t = Math.Clamp(Flat.Dot(point - a, d) / lengthSquared, 0, 1);
            double distance = Flat.Distance(point, a + d * t);
            if (distance < best)
            {
                best = distance;
                along = shapeAlong[i] + t * (shapeAlong[i + 1] - shapeAlong[i]);
                offset = Flat.Cross(d, point - a) / Math.Sqrt(lengthSquared);
            }
        }
        double length = streets[street].Length;
        return new StreetPosition(street, length <= 0 ? 0 : along / length, offset >= 0 ? Side.Left : Side.Right, offset, best);
    }

    public int Homes(int street, Side side) => homes[street * 2 + (int)side];

    public int Units(int street, Side side) => units[street * 2 + (int)side];

    public int Homes(int street) => homes[street * 2] + homes[street * 2 + 1];

    public LetterboxRange Letterboxes(int street, Side side)
    {
        int s = street * 2 + (int)side;
        return new LetterboxRange(boxStart[s], boxStart[s + 1]);
    }

    public Letterbox LetterboxAt(int letterbox)
    {
        int side = Array.BinarySearch(boxStart, letterbox);
        side = side >= 0 ? LastStartingAt(side) : ~side - 1;
        return new Letterbox(side / 2, (Side)(side % 2), boxAlong[letterbox], boxKind[letterbox], boxLabel[letterbox], boxKey[letterbox]);
    }

    public double LetterboxAlong(int letterbox) => boxAlong[letterbox];

    public LetterboxKind LetterboxKindOf(int letterbox) => boxKind[letterbox];

    public string? LetterboxLabel(int letterbox) => boxLabel[letterbox];

    public uint LetterboxAddressKey(int letterbox) => boxKey[letterbox];

    public LetterboxRange Between(int street, Side side, double from, double to)
    {
        LetterboxRange all = Letterboxes(street, side);
        (double low, double high) = from <= to ? (from, to) : (to, from);
        int first = LowerBound(all, low);
        int last = high >= 1 ? all.End : LowerBound(all, high);
        return new LetterboxRange(first, Math.Max(first, last));
    }

    public Flat LetterboxFlat(int letterbox, double setbackMetres = 0)
    {
        Letterbox box = LetterboxAt(letterbox);
        Flat centre = PointAt(box.Street, box.Along);
        double heading = HeadingAt(box.Street, box.Along) + (box.Side == Side.Left ? Math.PI / 2 : -Math.PI / 2);
        return centre + new Flat(Math.Cos(heading), Math.Sin(heading)) * setbackMetres;
    }

    public StreetIndex Index => index ??= new StreetIndex(this);

    internal int ShapeStart(int street) => shapeStart[street];

    internal double ShapeAlong(int point) => shapeAlong[point];

    internal Flat ShapePoint(int point) => shape[point];

    internal double[] NodeX => nodeX;

    internal double[] NodeY => nodeY;

    internal long[] NodeOsm => nodeOsm;

    internal StreetInfo[] Streets => streets;

    internal int[] ShapeStarts => shapeStart;

    internal Flat[] ShapeFlat => shape;

    internal int[] BoxStarts => boxStart;

    internal double[] BoxAlong => boxAlong;

    internal LetterboxKind[] BoxKinds => boxKind;

    internal string?[] BoxLabels => boxLabel;

    internal uint[] BoxKeys => boxKey;

    private int LastStartingAt(int side)
    {
        while (side + 1 < boxStart.Length - 1 && boxStart[side + 1] == boxStart[side])
            side++;
        return side;
    }

    private int LowerBound(LetterboxRange range, double at)
    {
        int low = range.Start, high = range.End;
        while (low < high)
        {
            int middle = (low + high) / 2;
            if (boxAlong[middle] < at)
                low = middle + 1;
            else
                high = middle;
        }
        return low;
    }

    private static (int[] Start, int[] Streets) Adjacency(int nodes, StreetInfo[] streets)
    {
        int[] start = new int[nodes + 1];
        foreach (StreetInfo street in streets)
        {
            start[street.U + 1]++;
            if (street.V != street.U)
                start[street.V + 1]++;
        }
        for (int n = 0; n < nodes; n++)
            start[n + 1] += start[n];
        int[] fill = start[..^1].ToArray();
        int[] list = new int[start[nodes]];
        for (int s = 0; s < streets.Length; s++)
        {
            list[fill[streets[s].U]++] = s;
            if (streets[s].V != streets[s].U)
                list[fill[streets[s].V]++] = s;
        }
        return (start, list);
    }
}

public readonly record struct LetterboxRange(int Start, int End)
{
    public int Count => End - Start;

    public bool Contains(int letterbox) => letterbox >= Start && letterbox < End;
}
