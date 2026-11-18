using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Places;

internal static class PlaceWords
{
    private static readonly string[] Compass = ["east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east"];

    private static readonly string[] KindOrder =
    [
        MapPlaceKinds.City, MapPlaceKinds.Town, MapPlaceKinds.Suburb, MapPlaceKinds.Village,
        MapPlaceKinds.Locality, MapPlaceKinds.Hamlet, MapPlaceKinds.Neighbourhood,
    ];

    public static double KmBetween(GeoPoint from, GeoPoint to) => new LocalProjection(from).Distance(from, to) / 1000;

    public static string Direction(GeoPoint from, GeoPoint to)
    {
        Flat offset = new LocalProjection(from).ToFlat(to);
        if (offset.Length < 1)
            return "";
        int sector = (int)Math.Round(offset.Bearing / (Math.PI / 4));
        return Compass[(sector % 8 + 8) % 8];
    }

    public static int Rank(PlaceRecord place, string search)
    {
        int match = place.Name.Equals(search, StringComparison.OrdinalIgnoreCase) ? 0
            : place.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase) ? 1
            : 2;
        int kind = Array.IndexOf(KindOrder, place.Kind) is var index and >= 0 ? index : KindOrder.Length;
        return match * 100 + kind;
    }
}
