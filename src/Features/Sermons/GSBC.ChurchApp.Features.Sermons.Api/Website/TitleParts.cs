using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Sermons.Api.Website;

internal static partial class TitleParts
{
    public static int PartOf(string title) =>
        PartNumber().Match(title) is { Success: true } match && int.TryParse(match.Groups["part"].Value, out int part) ? part : 0;

    public static string? SuggestedSeries(string title) =>
        SeriesBeforePart().Match(title) is { Success: true } match ? match.Groups["series"].Value.Trim() : null;

    [GeneratedRegex(@"\bPart\s+(?<part>\d+)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PartNumber();

    [GeneratedRegex(@"^(?<series>.+?)\s*(?::|-)\s*Part\s+\d+", RegexOptions.IgnoreCase)]
    private static partial Regex SeriesBeforePart();
}
