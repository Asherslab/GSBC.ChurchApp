using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;

internal static class GeoArea
{
    private const double MetresPerDegreeLatitude = 111_320;

    public static GeoBounds Around(GeoPoint centre, double halfWidthMetres) => new GeoBounds(centre.Lat, centre.Lon, centre.Lat, centre.Lon).Grown(halfWidthMetres);

    public static GeoBounds Grown(this GeoBounds bounds, double metres)
    {
        double latitude = metres / MetresPerDegreeLatitude;
        double longitude = metres / (MetresPerDegreeLatitude * Math.Cos(bounds.Centre.Lat * Math.PI / 180));
        return new GeoBounds(bounds.South - latitude, bounds.West - longitude, bounds.North + latitude, bounds.East + longitude);
    }

    public static double WidthMetres(this GeoBounds bounds) =>
        (bounds.East - bounds.West) * MetresPerDegreeLatitude * Math.Cos(bounds.Centre.Lat * Math.PI / 180);

    public static double HeightMetres(this GeoBounds bounds) => (bounds.North - bounds.South) * MetresPerDegreeLatitude;
}
