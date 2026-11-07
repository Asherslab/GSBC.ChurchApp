using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static partial class CalendarLinks
{
    public const string Public = "public";
    public const string Mine = "mine";
    public const int LongestSlug = 60;

    public static readonly IReadOnlyList<string> Reserved = [Public, Mine];

    public const string Extension = ".ics";
    public const string ContentType = "text/calendar; charset=utf-8";

    public static string CalendarPath(string slug) => $"/calendars/{slug}{Extension}";

    public static string PublicPath => CalendarPath(Public);

    public static string MinePath(string key) => $"/calendars/{Mine}/{key}{Extension}";

    public static string MinePath(string key, string slug) => $"/calendars/{Mine}/{key}/{slug}{Extension}";

    public static string Subscribe(Uri origin, string path) => new UriBuilder(new Uri(origin, path)) { Scheme = "webcal", Port = origin.IsDefaultPort ? -1 : origin.Port }.Uri.ToString();

    public static bool IsValidSlug(string slug) =>
        slug.Length is > 0 and <= LongestSlug && SlugPattern().IsMatch(slug) && !Reserved.Contains(slug);

    public static string SlugOf(string name)
    {
        string slug = NotSlug().Replace(name.Trim().ToLowerInvariant().Replace("&", "and", StringComparison.Ordinal), "-").Trim('-');
        slug = slug.Length > LongestSlug ? slug[..LongestSlug].TrimEnd('-') : slug;
        return Reserved.Contains(slug) ? $"{slug}-calendar" : slug;
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NotSlug();
}
