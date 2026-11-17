namespace GSBC.ChurchApp.Platform.Ui.Maps;

public readonly record struct MapPoint(double Lat, double Lon);

public sealed record MapSource(string Kind, string Location, string Attribution, string? Fallback = null)
{
    private const string FromUrl = "url";
    public const string FromKeptFile = "kept";
    public const string FromUrls = "urls";

    public static MapSource Url(string url, string attribution) => new(FromUrl, url, attribution);

    public static MapSource Urls(IEnumerable<string> urls, string attribution) => new(FromUrls, string.Join('\n', urls), attribution);

    public IReadOnlyList<string> Locations => Location.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    public static MapSource KeptFile(string id, string attribution, string? url = null) => new(FromKeptFile, id, attribution, url);
}

public sealed record MapBackdrop(string Url, MapFrame Bounds, int MaxZoom);

public enum MapLineStyle
{
    Walk,
    WalkDone,
    Walked,
    Connector,
    LongWalk,
    Route,
    Covered,
    Chosen,
}

public sealed record MapLine(IReadOnlyList<MapPoint> Points, MapLineStyle Style, string? Label = null, string? Colour = null, string? Id = null, double? Age = null);

public sealed record MapLineSet(string Key, IReadOnlyList<MapLine> Lines);

public sealed record MapArea(IReadOnlyList<MapPoint> Points, string? Colour = null, string? Id = null, bool Dashed = false, bool Outline = false, IReadOnlyList<IReadOnlyList<MapPoint>>? Holes = null,
    bool Hatched = false);

public enum MapMarkerKind
{
    Start,
    Pin,
    Meeting,
    NoJunkMail,
    DontLetterbox,
    Skip,
    Check,
    Note,
    Report,
    Match,
}

public sealed record MapMarker(MapPoint Point, MapMarkerKind Kind, string? Label = null, string? Id = null, bool Chosen = false, bool Corner = false, double ShownFrom = 0, double ShownUntil = 0);

public sealed record MapYou(MapPoint Point, double AccuracyMetres);

public sealed record MapFrame(double South, double West, double North, double East, int Padding = 40, int MaxZoom = 17, int StartPadding = 0, int BottomPadding = 0)
{
    public static MapFrame Around(IEnumerable<MapPoint> points, int padding = 40, int maxZoom = 17)
    {
        double south = double.MaxValue, west = double.MaxValue, north = double.MinValue, east = double.MinValue;
        foreach (MapPoint point in points)
        {
            south = Math.Min(south, point.Lat);
            north = Math.Max(north, point.Lat);
            west = Math.Min(west, point.Lon);
            east = Math.Max(east, point.Lon);
        }
        return south > north ? new MapFrame(0, 0, 0, 0, padding, maxZoom) : new MapFrame(south, west, north, east, padding, maxZoom);
    }

    private const double EarthMetres = 40_075_016.686;
    private const double TilePixels = 512;
    private const double MetresPerDegree = 111_320;

    public bool IsEmpty => South == 0 && North == 0 && West == 0 && East == 0;

    public MapPoint InsideCorner(double inset) => new(North - (North - South) * inset, West + (East - West) * inset);

    public double ZoomWhereItSpans(double pixels)
    {
        double latitude = (South + North) / 2 * Math.PI / 180;
        double side = Math.Min((East - West) * MetresPerDegree * Math.Cos(latitude), (North - South) * MetresPerDegree);
        return side <= 0 ? 0 : Math.Log2(pixels * EarthMetres * Math.Cos(latitude) / (TilePixels * side));
    }
}
