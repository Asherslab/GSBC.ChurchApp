using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;

internal static class Queensland
{
    public static readonly IReadOnlyList<GeoPoint> Outline =
    [
        new(-9, 138), new(-9, 154.5), new(-28.165, 154.5), new(-28.165, 153.55), new(-28.26, 153.0), new(-28.35, 152.72),
        new(-28.55, 152.3), new(-28.87, 152.03), new(-28.92, 151.93), new(-29.05, 151.28), new(-28.85, 151.17),
        new(-28.55, 150.31), new(-28.98, 148.99), new(-29, 141), new(-26, 141), new(-26, 138),
    ];

    public static bool Contains(GeoPoint point)
    {
        bool inside = false;
        for (int index = 0, previous = Outline.Count - 1; index < Outline.Count; previous = index++)
        {
            GeoPoint from = Outline[previous], to = Outline[index];
            if (from.Lat > point.Lat != to.Lat > point.Lat
                && point.Lon < from.Lon + (point.Lat - from.Lat) / (to.Lat - from.Lat) * (to.Lon - from.Lon))
                inside = !inside;
        }
        return inside;
    }
}
