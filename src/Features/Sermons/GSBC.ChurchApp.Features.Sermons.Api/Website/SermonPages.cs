using System.Net;
using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Sermons.Api.Website;

internal static partial class SermonPages
{
    public static IReadOnlyList<string> ArtworkOf(string html, string title, string? fileHost)
    {
        List<string> found = [];
        if (SharedImage().Match(html) is { Success: true } shared && TithelyArtwork.OriginalOf(WebUtility.HtmlDecode(shared.Groups["url"].Value), fileHost) is { } original)
            found.Add(original);
        foreach (Match image in Image().Matches(html))
        {
            string tag = image.Value;
            if (Attribute(tag, "alt") is { } alt && string.Equals(alt, title, StringComparison.OrdinalIgnoreCase)
                && Attribute(tag, "src") is { } src && src.StartsWith("https://", StringComparison.Ordinal))
                found.Add(src);
        }
        return found.Distinct().ToList();
    }

    private static string? Attribute(string tag, string name) =>
        Regex.Match(tag, $"""\s{name}="(?<value>[^"]*)" """.TrimEnd()) is { Success: true } match ? WebUtility.HtmlDecode(match.Groups["value"].Value).Trim() : null;

    [GeneratedRegex(""""<meta\s+property="og:image"\s+content="(?<url>https://[^"]+)"""")]
    private static partial Regex SharedImage();

    [GeneratedRegex("<img\\s[^>]*>")]
    private static partial Regex Image();
}
