using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

internal sealed class Streets
{
    public static readonly GeoPoint Origin = new(-27.34, 152.96);
    public static readonly LocalProjection Projection = new(Origin);

    private long _nextNode = 1;

    public OsmData Osm { get; } = new();

    public long Way(long id, string highway, string name, params (double X, double Y)[] metres) =>
        Way(id, new Dictionary<string, string> { ["highway"] = highway, ["name"] = name }, metres);

    public long Way(long id, Dictionary<string, string> tags, params (double X, double Y)[] metres)
    {
        long[] nodes = metres.Select(At).ToArray();
        Osm.Ways[id] = new OsmWay(id, nodes, tags);
        return id;
    }

    private long At((double X, double Y) metres)
    {
        foreach ((long id, GeoPoint point) in Osm.Nodes)
            if (Projection.Distance(point, Point(metres.X, metres.Y)) < 0.01)
                return id;
        long node = _nextNode++;
        Osm.Nodes[node] = Point(metres.X, metres.Y);
        return node;
    }

    public static GeoPoint Point(double x, double y) => Projection.ToGeo(new Flat(x, y));
}
