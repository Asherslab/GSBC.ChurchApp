using System.Net;
using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Sermons.Api.Website;

internal sealed record SeriesLink(string Slug, string Name);

internal sealed record SeriesPage(string? ArtworkUrl, IReadOnlyList<string> SermonPaths, IReadOnlyDictionary<string, string> SpeakerPhotos, IReadOnlyList<int> MorePages);

internal static partial class SeriesPages
{
    public const string IndexPath = "/podcasts/media";

    public static string PathOf(string slug) => $"/podcasts/media/series/{slug}";

    public static IReadOnlyList<SeriesLink> ReadIndex(string html) =>
        SeriesAnchor().Matches(html)
            .Select(match => new SeriesLink(match.Groups["slug"].Value, Clean(match.Groups["name"].Value)))
            .Where(link => link.Name.Length > 0)
            .DistinctBy(link => link.Slug)
            .ToList();

    public static SeriesPage ReadSeries(string html, string seriesName)
    {
        string? artwork = null;
        Dictionary<string, string> photos = new(StringComparer.OrdinalIgnoreCase);
        foreach (Match image in Image().Matches(html))
        {
            string tag = image.Value;
            string? alt = Attribute(tag, "alt");
            string? src = Attribute(tag, "src");
            if (alt is null || src is null || !src.StartsWith("https://", StringComparison.Ordinal))
                continue;
            if (artwork is null && string.Equals(alt, seriesName, StringComparison.OrdinalIgnoreCase))
                artwork = src;
            else if (Attribute(tag, "class")?.Contains("rounded-circle", StringComparison.Ordinal) == true)
                photos.TryAdd(alt, src);
        }

        List<string> sermons = SermonAnchor().Matches(html).Select(match => match.Groups["path"].Value).Distinct().ToList();
        List<int> pages = PageLink().Matches(html).Select(match => int.Parse(match.Groups["page"].Value)).Where(page => page > 1).Distinct().Order().ToList();
        return new SeriesPage(artwork, sermons, photos, pages);
    }

    private static string? Attribute(string tag, string name) =>
        Regex.Match(tag, $"""\s{name}="(?<value>[^"]*)" """.TrimEnd()) is { Success: true } match ? Clean(match.Groups["value"].Value) : null;

    private static string Clean(string text) => WebUtility.HtmlDecode(text).Trim();

    [GeneratedRegex("""href="/podcasts/media/series/(?<slug>[a-z0-9-]+)"[^>]*>(?<name>[^<]*)<""")]
    private static partial Regex SeriesAnchor();

    [GeneratedRegex(""""href="(?<path>/media/[a-z0-9-]+)"""")]
    private static partial Regex SermonAnchor();

    [GeneratedRegex("<img\\s[^>]*>")]
    private static partial Regex Image();

    [GeneratedRegex(""""href="[^"]*[?&]page=(?<page>\d{1,3})"""")]
    private static partial Regex PageLink();
}
