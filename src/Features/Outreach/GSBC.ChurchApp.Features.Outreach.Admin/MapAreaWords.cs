using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public static class MapAreaWords
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string State(MapArea area) => area.State switch
    {
        MapAreaStates.Waiting => "Waiting to build",
        MapAreaStates.Building => area.Stage is { Length: > 0 } stage ? $"Building: {stage}" : "Building",
        MapAreaStates.Failed => $"Couldn't build it{(area.Problem is { Length: > 0 } problem ? $": {problem}" : "")}. Rebuild it to try again.",
        _ => area.BuiltAt is { } built ? $"Built {OutreachWords.Date(built)}" : "Built",
    };

    public static string Regions(MapArea area)
    {
        long bytes = area.Regions.Sum(region => region.Streets.Bytes + region.Map.Bytes);
        int letterboxes = area.Regions.Sum(region => region.Letterboxes);
        long version = area.Regions.Count == 0 ? 0 : area.Regions.Max(region => region.Version);
        return $"{OutreachWords.Plural(area.Regions.Count, "region", "regions")} · {OutreachWords.Plural(letterboxes, "letterbox", "letterboxes")} · "
               + $"{Megabytes(bytes)} to download in all · version {version.ToString(Invariant)}";
    }

    public static string Megabytes(long bytes) => (bytes / 1_000_000.0).ToString(bytes < 10_000_000 ? "0.0" : "N0", Invariant) + " MB";

    public static (double Latitude, double Longitude)? Centre(string text)
    {
        string[] parts = text.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts is [var lat, var lng]
               && double.TryParse(lat, NumberStyles.Float, Invariant, out double latitude) && double.TryParse(lng, NumberStyles.Float, Invariant, out double longitude)
               && latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180
            ? (latitude, longitude)
            : null;
    }

    public static double? Radius(string text) =>
        double.TryParse(text.Trim(), NumberStyles.Float, Invariant, out double km) && km is >= 1 and <= 30 ? km : null;
}
