using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static partial class CalendarLinks
{
    public const string Public = "public";
    public const string Mine = "mine";
    public const int LongestSlug = 60;

    public static readonly IReadOnlyList<string> Reserved = [Public, Mine];

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
