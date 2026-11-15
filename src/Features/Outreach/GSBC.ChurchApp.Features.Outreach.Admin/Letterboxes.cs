using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Ui.Maps;
using RoutingPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public sealed class Letterboxes(StreetGraphLoader graphs)
{
    private const double ClickReachMetres = 60;
    private const double AddressReachMetres = 200;

    public async Task<AddressPoint?> NearestAsync(OutreachAtlas atlas, MapPoint point)
    {
        if (atlas.RegionAt(point) is not { } region)
            return null;
        StreetGraph graph = await graphs.LoadAsync(region);
        return Nearest(graph, point, ClickReachMetres) is { } letterbox ? Address(graph, region, letterbox) : null;
    }

    public async Task<AddressPoint?> ForAsync(OutreachAtlas atlas, AddressMatch match)
    {
        MapPoint point = new(match.Point.Lat, match.Point.Lng);
        if ((atlas.Regions.FirstOrDefault(region => region.Id == match.Region) ?? atlas.RegionAt(point)) is not { } region)
            return null;
        StreetGraph graph = await graphs.LoadAsync(region);
        int? letterbox = Keyed(graph, match.AddressKey) ?? Nearest(graph, point, AddressReachMetres);
        if (letterbox is not { } found)
            return null;
        AddressPoint address = Address(graph, region, found);
        address.Label = match.Label;
        return address;
    }

    private static int? Keyed(StreetGraph graph, uint key)
    {
        for (int letterbox = 0; letterbox < graph.LetterboxCount; letterbox++)
            if (graph.LetterboxAddressKey(letterbox) == key)
                return letterbox;
        return null;
    }

    private static int? Nearest(StreetGraph graph, MapPoint point, double reachMetres)
    {
        Flat flat = graph.Projection.ToFlat(new RoutingPoint(point.Lat, point.Lon));
        if (graph.Index.Nearest(flat, reachMetres, street => graph.Street(street).CanDeliver) is not { } near)
            return null;
        LetterboxRange boxes = graph.Letterboxes(near.Street, near.Side);
        int? nearest = null;
        double best = double.MaxValue;
        for (int box = boxes.Start; box < boxes.End; box++)
        {
            double distance = Math.Abs(graph.LetterboxAlong(box) - near.Along);
            if (distance < best)
                (best, nearest) = (distance, box);
        }
        return nearest;
    }

    private static AddressPoint Address(StreetGraph graph, OutreachRegion region, int letterbox)
    {
        AddressPoint address = EngineInputs.Address(graph, region.Id, letterbox);
        if (address.Label.Length == 0)
        {
            Letterbox box = graph.LetterboxAt(letterbox);
            address.Label = $"{graph.Street(box.Street).Name}, {(box.Side == Side.Left ? "left" : "right")} side";
        }
        return address;
    }
}
