using System.Net;
using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Website;

internal static partial class Html
{
    public static string Text(string html) => Spaces().Replace(WebUtility.HtmlDecode(Tag().Replace(html, " ")), " ").Trim();

    public static string? Attribute(string tag, string name) =>
        Regex.Match(tag, $"""\s{name}="(?<value>[^"]*)" """.TrimEnd()) is { Success: true } match ? WebUtility.HtmlDecode(match.Groups["value"].Value).Trim() : null;

    public static IEnumerable<string> Images(string html) => Image().Matches(html).Select(match => match.Value);

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex("<img\\s[^>]*>")]
    private static partial Regex Image();
}
