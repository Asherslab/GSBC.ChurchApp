namespace GSBC.ChurchApp.Platform.Ui.Maps;

public readonly record struct MapPoint(double Lat, double Lon);

public sealed record MapSource(string Kind, string Location, string Attribution)
{
    private const string FromUrl = "url";
    public const string FromKeptFile = "kept";
    public const string FromUrls = "urls";

    public static MapSource Url(string url, string attribution) => new(FromUrl, url, attribution);

    public static MapSource Urls(IEnumerable<string> urls, string attribution) => new(FromUrls, string.Join('\n', urls), attribution);

    public IReadOnlyList<string> Locations => Location.Split('\n');

    public static MapSource KeptFile(string id, string attribution) => new(FromKeptFile, id, attribution);
}

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

public sealed record MapArea(IReadOnlyList<MapPoint> Points, string? Colour = null, string? Id = null, bool Dashed = false, bool Outline = false, IReadOnlyList<IReadOnlyList<MapPoint>>? Holes = null);

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
}

public sealed record MapMarker(MapPoint Point, MapMarkerKind Kind, string? Label = null, string? Id = null, bool Chosen = false);

public sealed record MapYou(MapPoint Point, double AccuracyMetres);

public sealed record MapFrame(double South, double West, double North, double East, int Padding = 40, int MaxZoom = 17, int StartPadding = 0)
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

    public bool IsEmpty => South == 0 && North == 0 && West == 0 && East == 0;
}
