using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;

internal sealed record OsmWay(long Id, long[] Nodes, IReadOnlyDictionary<string, string> Tags)
{
    public string Highway => Tags.GetValueOrDefault("highway", "");

    public string Name => Tags.GetValueOrDefault("name", "");
}

internal sealed record OsmPlace(long Id, string Name, string Kind, GeoPoint Point, int Population);

internal sealed class OsmData
{
    public Dictionary<long, GeoPoint> Nodes { get; } = [];
    public Dictionary<long, OsmWay> Ways { get; } = [];

    public void Add(OsmData other)
    {
        foreach ((long id, GeoPoint point) in other.Nodes)
            Nodes[id] = point;
        foreach ((long id, OsmWay way) in other.Ways)
            Ways[id] = way;
    }
}
