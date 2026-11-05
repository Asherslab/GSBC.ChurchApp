using System.Net;
using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Sermons.Api.Website;

internal static partial class SermonPages
{
    public static string? ArtworkOf(string html, string? fileHost) =>
        SharedImage().Match(html) is { Success: true } match
            ? TithelyArtwork.OriginalOf(WebUtility.HtmlDecode(match.Groups["url"].Value), fileHost)
            : null;

    [GeneratedRegex(""""<meta\s+property="og:image"\s+content="(?<url>https://[^"]+)"""")]
    private static partial Regex SharedImage();
}
