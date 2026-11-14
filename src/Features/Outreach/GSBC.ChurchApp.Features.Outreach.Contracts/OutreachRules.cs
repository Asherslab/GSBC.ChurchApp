using System.Globalization;

namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public static class OutreachRules
{
    public const int WholeStreet = 1000;
    public const int LongestName = 80;
    public const int LongestNote = 1000;
    public const int LongestFirstName = 40;
    public const int LongestContact = 120;
    public const string IsoDate = "yyyy-MM-dd";

    public static bool IsValid(Stretch stretch) =>
        stretch.Region.Length is > 0 and <= 80 && stretch.Street.Length is > 0 and <= 80 && Enum.IsDefined(stretch.Side)
        && stretch.From >= 0 && stretch.To <= WholeStreet && stretch.From < stretch.To && stretch.Metres >= 0 && stretch.Letterboxes >= 0;

    public static string Clip(string? text, int longest)
    {
        string trimmed = (text ?? "").Trim();
        return trimmed.Length <= longest ? trimmed : trimmed[..longest];
    }

    public static DateOnly? DateOf(string? text) =>
        DateOnly.TryParseExact(text, IsoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : null;

    public static string Format(DateOnly date) => date.ToString(IsoDate, CultureInfo.InvariantCulture);

    public static bool IsActive(Campaign campaign, DateOnly today) =>
        campaign.General
        || ((DateOf(campaign.StartsOn) is not { } starts || starts <= today) && (DateOf(campaign.EndsOn) is not { } ends || ends >= today));

    public static bool Contains(IReadOnlyList<GeoPoint> polygon, GeoPoint point)
    {
        if (polygon.Count < 3)
            return false;
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            GeoPoint a = polygon[i];
            GeoPoint b = polygon[j];
            if ((a.Lat > point.Lat) != (b.Lat > point.Lat)
                && point.Lng < (b.Lng - a.Lng) * (point.Lat - a.Lat) / (b.Lat - a.Lat) + a.Lng)
                inside = !inside;
        }
        return inside;
    }

    public static bool IsAPolygon(IReadOnlyList<GeoPoint> polygon) =>
        polygon.Count is >= 3 and <= 2000 && polygon.All(point => point.Lat is >= -90 and <= 90 && point.Lng is >= -180 and <= 180);
}
