using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Client;

public static class OutreachRegions
{
    public static OutreachRegion? For(IReadOnlyList<OutreachRegion> regions, double lat, double lon) =>
        regions.FirstOrDefault(region => Inside(region.Core, lat, lon))
        ?? regions.Where(region => region.Streets.Url.Length > 0).MinBy(region => Distance(region.Core, lat, lon));

    public static (double Lat, double Lon) Centre(OutreachRegion region) => ((region.Core.South + region.Core.North) / 2, (region.Core.West + region.Core.East) / 2);

    private static bool Inside(MapBounds bounds, double lat, double lon) => lat >= bounds.South && lat <= bounds.North && lon >= bounds.West && lon <= bounds.East;

    private static double Distance(MapBounds bounds, double lat, double lon)
    {
        double dLat = lat - (bounds.South + bounds.North) / 2, dLon = lon - (bounds.West + bounds.East) / 2;
        return dLat * dLat + dLon * dLon;
    }
}
