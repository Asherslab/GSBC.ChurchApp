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
    private readonly double[] _nodeX;
    private readonly double[] _nodeY;
    private readonly long[] _nodeOsm;
    private readonly StreetInfo[] _streets;
    private readonly int[] _shapeStart;
    private readonly Flat[] _shape;
    private readonly double[] _shapeAlong;
    private readonly int[] _boxStart;
    private readonly double[] _boxAlong;
    private readonly LetterboxKind[] _boxKind;
    private readonly string?[] _boxLabel;
    private readonly uint[] _boxKey;
    private readonly int[] _homes;
    private readonly int[] _units;
    private readonly int[] _adjacentStart;
    private readonly int[] _adjacent;
    private Dictionary<StreetKey, int>? _byKey;

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
        _nodeX = nodeX;
        _nodeY = nodeY;
        _nodeOsm = nodeOsm;
        _streets = streets;
        _shapeStart = shapeStart;
        _shape = shape;
        _boxStart = boxStart;
        _boxAlong = boxAlong;
        _boxKind = boxKind;
        _boxLabel = boxLabel;
        _boxKey = boxKey;
        _shapeAlong = new double[shape.Length];
        for (int s = 0; s < streets.Length; s++)
        {
            int from = shapeStart[s], to = shapeStart[s + 1];
            for (int i = from + 1; i < to; i++)
                _shapeAlong[i] = _shapeAlong[i - 1] + Flat.Distance(shape[i - 1], shape[i]);
        }
        _homes = new int[streets.Length * 2];
        _units = new int[streets.Length * 2];
        for (int side = 0; side < _homes.Length; side++)
        {
            for (int b = boxStart[side]; b < boxStart[side + 1]; b++)
            {
                _homes[side] += boxKind[b].IsHome() ? 1 : 0;
                _units[side] += boxKind[b] == LetterboxKind.Unit ? 1 : 0;
            }
        }
        (_adjacentStart, _adjacent) = Adjacency(nodeX.Length, streets);
    }

    public StreetRegion Region { get; }

    public LocalProjection Projection { get; }

    public int NodeCount => _nodeX.Length;

    public int StreetCount => _streets.Length;

    public int LetterboxCount => _boxAlong.Length;

    public int ShapePointCount => _shape.Length;

    public Flat NodeFlat(int node) => new(_nodeX[node], _nodeY[node]);

    public GeoPoint NodePoint(int node) => Projection.ToGeo(NodeFlat(node));

    public long NodeOsmId(int node) => _nodeOsm[node];

    public ref readonly StreetInfo Street(int street) => ref _streets[street];

    public StreetKey KeyOf(int street) => new(_streets[street].Way, _nodeOsm[_streets[street].U], _nodeOsm[_streets[street].V]);

    public int? StreetOf(StreetKey key)
    {
        _byKey ??= Enumerable.Range(0, _streets.Length).GroupBy(KeyOf).ToDictionary(g => g.Key, g => g.First());
        return _byKey.TryGetValue(key, out int street) ? street : null;
    }

    public ReadOnlySpan<int> StreetsAt(int node) => _adjacent.AsSpan(_adjacentStart[node], _adjacentStart[node + 1] - _adjacentStart[node]);

    public int OtherEnd(int street, int node) => _streets[street].U == node ? _streets[street].V : _streets[street].U;

    public ReadOnlySpan<Flat> Shape(int street) => _shape.AsSpan(_shapeStart[street], _shapeStart[street + 1] - _shapeStart[street]);

    public IReadOnlyList<GeoPoint> ShapePoints(int street, double from = 0, double to = 1) =>
        ShapeBetween(street, from, to).Select(Projection.ToGeo).ToList();

    public IReadOnlyList<Flat> ShapeBetween(int street, double from = 0, double to = 1)
    {
        List<Flat> points = [PointAt(street, Math.Min(from, to))];
        double length = _streets[street].Length;
        for (int i = _shapeStart[street] + 1; i < _shapeStart[street + 1] - 1; i++)
        {
            double at = length <= 0 ? 0 : _shapeAlong[i] / length;
            if (at > Math.Min(from, to) && at < Math.Max(from, to))
                points.Add(_shape[i]);
        }
        points.Add(PointAt(street, Math.Max(from, to)));
        if (from > to)
            points.Reverse();
        return points;
    }

    public Flat PointAt(int street, double along)
    {
        int from = _shapeStart[street], to = _shapeStart[street + 1];
        double target = Math.Clamp(along, 0, 1) * _streets[street].Length;
        for (int i = from; i < to - 1; i++)
        {
            if (target <= _shapeAlong[i + 1] || i == to - 2)
            {
                double span = _shapeAlong[i + 1] - _shapeAlong[i];
                double t = span <= 0 ? 0 : Math.Clamp((target - _shapeAlong[i]) / span, 0, 1);
                return _shape[i] + (_shape[i + 1] - _shape[i]) * t;
            }
        }
        return _shape[from];
    }

    public double HeadingAt(int street, double along)
    {
        int from = _shapeStart[street], to = _shapeStart[street + 1];
        double target = Math.Clamp(along, 0, 1) * _streets[street].Length;
        for (int i = from; i < to - 1; i++)
        {
            if (target <= _shapeAlong[i + 1] || i == to - 2)
                return (_shape[i + 1] - _shape[i]).Bearing;
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
        int from = _shapeStart[street], to = _shapeStart[street + 1];
        double best = double.MaxValue, along = 0, offset = 0;
        for (int i = from; i < to - 1; i++)
        {
            Flat a = _shape[i], d = _shape[i + 1] - a;
            double lengthSquared = Math.Max(Flat.Dot(d, d), 1e-9);
            double t = Math.Clamp(Flat.Dot(point - a, d) / lengthSquared, 0, 1);
            double distance = Flat.Distance(point, a + d * t);
            if (distance < best)
            {
                best = distance;
                along = _shapeAlong[i] + t * (_shapeAlong[i + 1] - _shapeAlong[i]);
                offset = Flat.Cross(d, point - a) / Math.Sqrt(lengthSquared);
            }
        }
        double length = _streets[street].Length;
        return new StreetPosition(street, length <= 0 ? 0 : along / length, offset >= 0 ? Side.Left : Side.Right, offset, best);
    }

    public int Homes(int street, Side side) => _homes[street * 2 + (int)side];

    public int Units(int street, Side side) => _units[street * 2 + (int)side];

    public int Homes(int street) => _homes[street * 2] + _homes[street * 2 + 1];

    public LetterboxRange Letterboxes(int street, Side side)
    {
        int s = street * 2 + (int)side;
        return new LetterboxRange(_boxStart[s], _boxStart[s + 1]);
    }

    public Letterbox LetterboxAt(int letterbox)
    {
        int side = Array.BinarySearch(_boxStart, letterbox);
        side = side >= 0 ? LastStartingAt(side) : ~side - 1;
        return new Letterbox(side / 2, (Side)(side % 2), _boxAlong[letterbox], _boxKind[letterbox], _boxLabel[letterbox], _boxKey[letterbox]);
    }

    public double LetterboxAlong(int letterbox) => _boxAlong[letterbox];

    public LetterboxKind LetterboxKindOf(int letterbox) => _boxKind[letterbox];

    public string? LetterboxLabel(int letterbox) => _boxLabel[letterbox];

    public uint LetterboxAddressKey(int letterbox) => _boxKey[letterbox];

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

    public StreetIndex Index => field ??= new StreetIndex(this);

    internal int ShapeStart(int street) => _shapeStart[street];

    internal double ShapeAlong(int point) => _shapeAlong[point];

    internal Flat ShapePoint(int point) => _shape[point];

    internal double[] NodeX => _nodeX;

    internal double[] NodeY => _nodeY;

    internal long[] NodeOsm => _nodeOsm;

    internal StreetInfo[] Streets => _streets;

    internal int[] ShapeStarts => _shapeStart;

    internal Flat[] ShapeFlat => _shape;

    internal int[] BoxStarts => _boxStart;

    internal double[] BoxAlong => _boxAlong;

    internal LetterboxKind[] BoxKinds => _boxKind;

    internal string?[] BoxLabels => _boxLabel;

    internal uint[] BoxKeys => _boxKey;

    private int LastStartingAt(int side)
    {
        while (side + 1 < _boxStart.Length - 1 && _boxStart[side + 1] == _boxStart[side])
            side++;
        return side;
    }

    private int LowerBound(LetterboxRange range, double at)
    {
        int low = range.Start, high = range.End;
        while (low < high)
        {
            int middle = (low + high) / 2;
            if (_boxAlong[middle] < at)
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
