using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Website;

internal sealed record WebsiteSeries(string Slug, string Name, string Description, string? ArtworkUrl);

internal static partial class SeriesPage
{
    public static string PathOf(string slug) => $"/series/{slug}";

    public static WebsiteSeries Read(string slug, string fallbackName, string html)
    {
        if (Show().Match(html) is not { Success: true } show)
            return new WebsiteSeries(slug, fallbackName, "", null);
        string side = show.Groups["side"].Value;
        string? picture = Html.Images(side).FirstOrDefault();
        string name = picture is not null && Html.Attribute(picture, "alt") is { Length: > 0 } alt ? alt : fallbackName;
        string? artwork = picture is not null ? Html.Attribute(picture, "src") : null;
        string description = Heading().Match(side) is { Success: true } heading
            ? string.Join("\n\n", Paragraph().Split(heading.Groups["text"].Value).Select(Html.Text).Where(text => text.Length > 0))
            : "";
        return new WebsiteSeries(slug, name, description, artwork is not null && artwork.StartsWith("https://", StringComparison.Ordinal) ? artwork : null);
    }

    [GeneratedRegex("""<div class="series-show">\s*<div class="row">\s*<div class="col-sm-5[^"]*">(?<side>.*?)<div class="col-sm-7""", RegexOptions.Singleline)]
    private static partial Regex Show();

    [GeneratedRegex("""<div class="pageheading">(?<text>.*)</div>""", RegexOptions.Singleline)]
    private static partial Regex Heading();

    [GeneratedRegex("</p>|<br\\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex Paragraph();
}
