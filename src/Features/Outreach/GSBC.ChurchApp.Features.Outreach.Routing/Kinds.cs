using System.Globalization;

namespace GSBC.ChurchApp.Features.Outreach.Routing;

public enum RoadKind : byte
{
    Residential,
    LivingStreet,
    Unclassified,
    Tertiary,
    Secondary,
    Primary,
    Trunk,
    Link,
    Service,
    Footway,
    Path,
    Cycleway,
    Pedestrian,
    Steps,
    Track,
}

public static class RoadKinds
{
    public static bool IsMainRoad(this RoadKind kind) => kind is RoadKind.Trunk or RoadKind.Primary or RoadKind.Secondary or RoadKind.Link;

    public static bool IsPath(this RoadKind kind) =>
        kind is RoadKind.Footway or RoadKind.Path or RoadKind.Cycleway or RoadKind.Pedestrian or RoadKind.Steps or RoadKind.Track;

    public static bool CanDeliver(this RoadKind kind) => !kind.IsMainRoad() && !kind.IsPath();

    public static RoadKind FromOsm(string highway) => highway switch
    {
        "residential" => RoadKind.Residential,
        "living_street" => RoadKind.LivingStreet,
        "unclassified" => RoadKind.Unclassified,
        "tertiary" => RoadKind.Tertiary,
        "secondary" => RoadKind.Secondary,
        "primary" => RoadKind.Primary,
        "trunk" or "motorway" => RoadKind.Trunk,
        _ when highway.EndsWith("_link", StringComparison.Ordinal) => RoadKind.Link,
        "service" => RoadKind.Service,
        "footway" => RoadKind.Footway,
        "cycleway" => RoadKind.Cycleway,
        "pedestrian" => RoadKind.Pedestrian,
        "steps" => RoadKind.Steps,
        "track" or "bridleway" => RoadKind.Track,
        _ => RoadKind.Path,
    };
}

public enum Side : byte
{
    Left,
    Right,
}

public enum LetterboxKind : byte
{
    House,
    Unit,
    Check,
    Skip,
}

public static class LetterboxKinds
{
    public static bool IsHome(this LetterboxKind kind) => kind != LetterboxKind.Skip;
}

public readonly record struct StreetKey(long Way, long From, long To)
{
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Way}/{From}/{To}");

    public static bool TryParse(string? text, out StreetKey key)
    {
        string[] parts = text?.Split('/') ?? [];
        if (parts.Length == 3
            && long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long way)
            && long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long from)
            && long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long to))
        {
            key = new StreetKey(way, from, to);
            return true;
        }
        key = default;
        return false;
    }
}
