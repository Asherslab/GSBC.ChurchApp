namespace GSBC.ChurchApp.Features.Outreach.Routing;

public sealed class StreetGraphBuilder(StreetRegion region)
{
    private readonly LocalProjection projection = new(region.Origin);
    private readonly List<double> nodeX = [];
    private readonly List<double> nodeY = [];
    private readonly List<long> nodeOsm = [];
    private readonly List<(int U, int V, string Name, string FullName, RoadKind Kind, long Way, Flat[] Inner)> streets = [];
    private readonly List<(int Street, Side Side, double Along, LetterboxKind Kind, string? Label, uint Key)> boxes = [];

    public int AddNode(GeoPoint point, long osmId = 0)
    {
        Flat flat = StreetGraphFile.QuantizeFlat(projection.ToFlat(point));
        nodeX.Add(flat.X);
        nodeY.Add(flat.Y);
        nodeOsm.Add(osmId);
        return nodeX.Count - 1;
    }

    public int AddStreet(int u, int v, IReadOnlyList<GeoPoint> inner, string name, RoadKind kind, long way = 0, string? fullName = null)
    {
        if (u < 0 || u >= nodeX.Count || v < 0 || v >= nodeX.Count)
            throw new ArgumentOutOfRangeException(nameof(u), "Add both end nodes before the street.");
        streets.Add((u, v, name, fullName ?? name, kind, way, inner.Select(point => StreetGraphFile.QuantizeFlat(projection.ToFlat(point))).ToArray()));
        return streets.Count - 1;
    }

    public void AddLetterbox(int street, Side side, double along, LetterboxKind kind, string? label = null, uint addressKey = 0)
    {
        if (street < 0 || street >= streets.Count)
            throw new ArgumentOutOfRangeException(nameof(street));
        boxes.Add((street, side, StreetGraphFile.QuantizeAlong(along), kind, label, addressKey));
    }

    public StreetGraph Build()
    {
        int[] shapeStart = new int[streets.Count + 1];
        List<Flat> shape = [];
        StreetInfo[] infos = new StreetInfo[streets.Count];
        for (int s = 0; s < streets.Count; s++)
        {
            (int u, int v, string name, string fullName, RoadKind kind, long way, Flat[] inner) = streets[s];
            shapeStart[s] = shape.Count;
            shape.Add(new Flat(nodeX[u], nodeY[u]));
            shape.AddRange(inner);
            shape.Add(new Flat(nodeX[v], nodeY[v]));
            double length = 0;
            for (int i = shapeStart[s] + 1; i < shape.Count; i++)
                length += Flat.Distance(shape[i - 1], shape[i]);
            infos[s] = new StreetInfo(u, v, name, fullName, kind, way, length);
        }
        shapeStart[streets.Count] = shape.Count;

        var ordered = boxes.OrderBy(b => b.Street).ThenBy(b => b.Side).ThenBy(b => b.Along).ToArray();
        int[] boxStart = new int[streets.Count * 2 + 1];
        foreach (var box in ordered)
            boxStart[box.Street * 2 + (int)box.Side + 1]++;
        for (int i = 0; i < streets.Count * 2; i++)
            boxStart[i + 1] += boxStart[i];

        return new StreetGraph(
            region,
            [.. nodeX],
            [.. nodeY],
            [.. nodeOsm],
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
