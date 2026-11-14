namespace GSBC.ChurchApp.Features.Outreach.Routing;

public readonly record struct GeoPoint(double Lat, double Lon);

public readonly record struct GeoBounds(double South, double West, double North, double East)
{
    public GeoPoint Centre => new((South + North) / 2, (West + East) / 2);

    public bool Contains(GeoPoint point) =>
        point.Lat >= South && point.Lat <= North && point.Lon >= West && point.Lon <= East;

    public static GeoBounds Around(IEnumerable<GeoPoint> points)
    {
        double south = double.MaxValue, west = double.MaxValue, north = double.MinValue, east = double.MinValue;
        foreach (GeoPoint point in points)
        {
            south = Math.Min(south, point.Lat);
            north = Math.Max(north, point.Lat);
            west = Math.Min(west, point.Lon);
            east = Math.Max(east, point.Lon);
        }
        return new GeoBounds(south, west, north, east);
    }
}

public readonly record struct Flat(double X, double Y)
{
    public static Flat operator -(Flat a, Flat b) => new(a.X - b.X, a.Y - b.Y);

    public static Flat operator +(Flat a, Flat b) => new(a.X + b.X, a.Y + b.Y);

    public static Flat operator *(Flat a, double k) => new(a.X * k, a.Y * k);

    public double Length => Math.Sqrt(X * X + Y * Y);

    public static double Dot(Flat a, Flat b) => a.X * b.X + a.Y * b.Y;

    public static double Cross(Flat a, Flat b) => a.X * b.Y - a.Y * b.X;

    public static double Distance(Flat a, Flat b) => (a - b).Length;

    public double Bearing => Math.Atan2(Y, X);
}

public sealed class LocalProjection(GeoPoint origin)
{
    private const double EarthMetres = 6_371_000;
    private const double MetresPerDegree = EarthMetres * Math.PI / 180;

    private readonly double _cosLat = Math.Cos(origin.Lat * Math.PI / 180);

    public GeoPoint Origin => origin;

    public Flat ToFlat(GeoPoint point) =>
        new((point.Lon - origin.Lon) * MetresPerDegree * _cosLat, (point.Lat - origin.Lat) * MetresPerDegree);

    public GeoPoint ToGeo(Flat flat) =>
        new(origin.Lat + flat.Y / MetresPerDegree, origin.Lon + flat.X / (MetresPerDegree * _cosLat));

    public double Distance(GeoPoint a, GeoPoint b) => Flat.Distance(ToFlat(a), ToFlat(b));
}
