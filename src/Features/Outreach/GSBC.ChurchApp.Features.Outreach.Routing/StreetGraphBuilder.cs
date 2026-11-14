namespace GSBC.ChurchApp.Features.Outreach.Routing;

public sealed class StreetGraphBuilder(StreetRegion region)
{
    private readonly LocalProjection _projection = new(region.Origin);
    private readonly List<double> _nodeX = [];
    private readonly List<double> _nodeY = [];
    private readonly List<long> _nodeOsm = [];
    private readonly List<(int U, int V, string Name, string FullName, RoadKind Kind, long Way, Flat[] Inner)> _streets = [];
    private readonly List<(int Street, Side Side, double Along, LetterboxKind Kind, string? Label, uint Key)> _boxes = [];

    public int AddNode(GeoPoint point, long osmId = 0)
    {
        Flat flat = StreetGraphFile.QuantizeFlat(_projection.ToFlat(point));
        _nodeX.Add(flat.X);
        _nodeY.Add(flat.Y);
        _nodeOsm.Add(osmId);
        return _nodeX.Count - 1;
    }

    public int AddStreet(int u, int v, IReadOnlyList<GeoPoint> inner, string name, RoadKind kind, long way = 0, string? fullName = null)
    {
        if (u < 0 || u >= _nodeX.Count || v < 0 || v >= _nodeX.Count)
            throw new ArgumentOutOfRangeException(nameof(u), "Add both end nodes before the street.");
        _streets.Add((u, v, name, fullName ?? name, kind, way, inner.Select(point => StreetGraphFile.QuantizeFlat(_projection.ToFlat(point))).ToArray()));
        return _streets.Count - 1;
    }

    public void AddLetterbox(int street, Side side, double along, LetterboxKind kind, string? label = null, uint addressKey = 0)
    {
        if (street < 0 || street >= _streets.Count)
            throw new ArgumentOutOfRangeException(nameof(street));
        _boxes.Add((street, side, StreetGraphFile.QuantizeAlong(along), kind, label, addressKey));
    }

    public StreetGraph Build()
    {
        int[] shapeStart = new int[_streets.Count + 1];
        List<Flat> shape = [];
        StreetInfo[] infos = new StreetInfo[_streets.Count];
        for (int s = 0; s < _streets.Count; s++)
        {
            (int u, int v, string name, string fullName, RoadKind kind, long way, Flat[] inner) = _streets[s];
            shapeStart[s] = shape.Count;
            shape.Add(new Flat(_nodeX[u], _nodeY[u]));
            shape.AddRange(inner);
            shape.Add(new Flat(_nodeX[v], _nodeY[v]));
            double length = 0;
            for (int i = shapeStart[s] + 1; i < shape.Count; i++)
                length += Flat.Distance(shape[i - 1], shape[i]);
            infos[s] = new StreetInfo(u, v, name, fullName, kind, way, length);
        }
        shapeStart[_streets.Count] = shape.Count;

        var ordered = _boxes.OrderBy(b => b.Street).ThenBy(b => b.Side).ThenBy(b => b.Along).ToArray();
        int[] boxStart = new int[_streets.Count * 2 + 1];
        foreach (var box in ordered)
            boxStart[box.Street * 2 + (int)box.Side + 1]++;
        for (int i = 0; i < _streets.Count * 2; i++)
            boxStart[i + 1] += boxStart[i];

        return new StreetGraph(
            region,
            [.. _nodeX],
            [.. _nodeY],
            [.. _nodeOsm],
            infos,
            shapeStart,
            [.. shape],
            boxStart,
            ordered.Select(b => b.Along).ToArray(),
            ordered.Select(b => b.Kind).ToArray(),
            ordered.Select(b => b.Label).ToArray(),
            ordered.Select(b => b.Key).ToArray());
    }
}
