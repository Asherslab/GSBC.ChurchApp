using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.LandUse;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal sealed record PlacedLetterbox(int Segment, Side Side, double Along, LetterboxKind Kind, string? Label, uint AddressKey, string Locality, GeoPoint Point);

internal sealed record PlacementCounts(int Addresses, int Placed, int Homes, int Checks, int Skipped);

internal sealed class AreaStreets
{
    private readonly OsmData _osm;

    private AreaStreets(OsmData osm, Network network, List<PlacedLetterbox> letterboxes, PlacementCounts counts)
    {
        _osm = osm;
        Network = network;
        Letterboxes = letterboxes;
        Counts = counts;
    }

    public Network Network { get; }
    private IReadOnlyList<PlacedLetterbox> Letterboxes { get; }
    public PlacementCounts Counts { get; }

    public static AreaStreets Build(OsmData osm, IReadOnlyList<AddressPoint> addresses, LandUseMap landUse, GeoPoint origin)
    {
        LocalProjection projection = new(origin);
        Network network = StreetNetwork.Build(osm, projection);
        AddressPlacement placement = new(network.Segments, network.Segments.Select(segment => StreetNetwork.Shape(segment, osm, projection)).ToArray());
        List<PlacedLetterbox> letterboxes = [];
        foreach (AddressPoint address in addresses)
        {
            if (placement.Place(projection.ToFlat(address.Point), address.Street) is not { } placed)
                continue;
            LandUseVerdict verdict = landUse.Classify(address);
            LetterboxKind kind = verdict.Delivery switch
            {
                Delivery.Skip => LetterboxKind.Skip,
                Delivery.Check => LetterboxKind.Check,
                _ when address.IsUnit => LetterboxKind.Unit,
                _ => LetterboxKind.House,
            };
            letterboxes.Add(new PlacedLetterbox(placed.Segment, placed.Side, placed.Along, kind, verdict.Label, address.Id, address.Locality, address.Point));
        }
        int homes = letterboxes.Count(box => box.Kind.IsHome());
        PlacementCounts counts = new(addresses.Count, letterboxes.Count, homes,
            letterboxes.Count(box => box.Kind == LetterboxKind.Check), letterboxes.Count - homes);
        return new AreaStreets(osm, network, letterboxes, counts);
    }

    public StreetGraph GraphFor(RegionCell cell, StreetRegion region)
    {
        StreetGraphBuilder builder = new(region);
        Dictionary<long, int> nodes = [];
        Dictionary<int, int> streets = [];
        for (int s = 0; s < Network.Segments.Count; s++)
        {
            StreetSegment segment = Network.Segments[s];
            if (!segment.Nodes.Any(node => cell.Bounds.Contains(_osm.Nodes[node])))
                continue;
            int u = NodeOf(segment.Nodes[0]);
            int v = NodeOf(segment.Nodes[^1]);
            GeoPoint[] inner = segment.Nodes[1..^1].Select(node => _osm.Nodes[node]).ToArray();
            string name = segment.Name.Length == 0 ? "" : StreetNames.Short(segment.Name);
            streets[s] = builder.AddStreet(u, v, inner, name, segment.Kind, segment.Way, segment.Name);
        }
        foreach (PlacedLetterbox box in Letterboxes)
            if (streets.TryGetValue(box.Segment, out int street))
                builder.AddLetterbox(street, box.Side, box.Along, box.Kind, box.Label, box.AddressKey);
        return builder.Build();

        int NodeOf(long osmId)
        {
            if (!nodes.TryGetValue(osmId, out int index))
                nodes[osmId] = index = builder.AddNode(_osm.Nodes[osmId], osmId);
            return index;
        }
    }

    public int HomesIn(GeoBounds core) => Letterboxes.Count(box => box.Kind.IsHome() && core.Contains(box.Point));

    public string NameFor(GeoBounds core)
    {
        List<(string Locality, int Count)> localities = Letterboxes.Where(box => box.Kind.IsHome() && core.Contains(box.Point) && box.Locality.Length > 0)
            .GroupBy(box => Suburb(box.Locality)).Select(group => (group.Key, group.Count()))
            .OrderByDescending(locality => locality.Item2).ThenBy(locality => locality.Key, StringComparer.Ordinal).ToList();
        return localities switch
        {
            [] => "",
            [var first, var second, ..] when second.Count * 3 >= first.Count => $"{first.Locality} and {second.Locality}",
            [var first, ..] => first.Locality,
        };
    }

    public static string Suburb(string locality) => locality.Split(" (")[0];
}
