using System.Globalization;
using System.Text.RegularExpressions;
using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Website;

internal sealed record WebsiteSeriesLink(string Slug, string Name);

internal sealed record WebsiteSermon(
    string Path,
    int? WebsiteId,
    string Title,
    DateOnly PreachedOn,
    string ServiceLabel,
    string? Speaker,
    string? SpeakerPhotoUrl,
    WebsiteSeriesLink? Series,
    string AudioUrl,
    string? YouTubeId,
    string Passage,
    string Description,
    IReadOnlyList<string> ArtworkUrls);

internal static partial class SermonPages
{
    private const string GuestSpeaker = "Guest Speaker";
    private const string SoundCloudCaption = " by Good Shepherd Baptist Church";

    public static string PathOf(string listedPath) => $"/podcasts{listedPath}";

    public static WebsiteSermon? Read(string listedPath, string html)
    {
        if (Heading().Match(html) is not { Success: true } heading || Audio().Match(html) is not { Success: true } audio)
            return null;
        string title = Html.Text(heading.Groups["title"].Value);
        (string? speaker, string? photo, DateOnly? preachedOn) = Byline(html);
        if (title.Length == 0 || preachedOn is null)
            return null;

        string description = Description(html);
        return new WebsiteSermon(
            listedPath,
            WebsiteIdOf(html),
            title,
            preachedOn.Value,
            ServiceLabel(html),
            speaker,
            photo,
            SeriesCrumb().Match(html) is { Success: true } series ? new WebsiteSeriesLink(series.Groups["slug"].Value, Html.Text(series.Groups["name"].Value)) : null,
            Html.Text(audio.Groups["url"].Value),
            Frame().Matches(html).Select(frame => YouTubeIds.Find(frame.Groups["url"].Value)).FirstOrDefault(id => id is not null),
            Reference().Match(html) is { Success: true } reference ? Html.Text(reference.Groups["passage"].Value) : "",
            description,
            Artwork(html, title));
    }

    private static int? WebsiteIdOf(string html) =>
        MediaClass().Match(html) is { Success: true } match && int.TryParse(match.Groups["id"].Value, out int id) ? id : null;

    private static string ServiceLabel(string html)
    {
        if (Subheading().Match(html) is not { Success: true } match)
            return "";
        string label = Html.Text(match.Groups["label"].Value);
        return label.EndsWith('…') || label.EndsWith("...", StringComparison.Ordinal) ? "" : label;
    }

    private static (string? Speaker, string? Photo, DateOnly? PreachedOn) Byline(string html)
    {
        if (BylineBlock().Match(html) is not { Success: true } block)
            return (null, null, null);
        string byline = block.Groups["byline"].Value;
        string? photoTag = Html.Images(byline).FirstOrDefault();
        List<string> words = TextNode().Matches(Image().Replace(byline, ""))
            .Select(node => Html.Text(node.Groups["text"].Value)).Where(text => text.Length > 0).ToList();
        DateOnly? day = words.Count > 0 && DayOf(words[^1]) is { } last ? last : null;
        string? name = photoTag is not null ? Html.Attribute(photoTag, "alt") : words.Count > 1 ? words[0] : null;
        if (string.IsNullOrEmpty(name) || string.Equals(name, GuestSpeaker, StringComparison.OrdinalIgnoreCase))
            return (null, null, day);
        string? photo = photoTag is not null ? Html.Attribute(photoTag, "src") : null;
        return (name, photo is not null && photo.StartsWith("https://", StringComparison.Ordinal) ? photo : null, day);
    }

    private static DateOnly? DayOf(string text) =>
        DateOnly.TryParseExact(Html.Text(text), "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day) ? day : null;

    private static string Description(string html)
    {
        if (Article().Match(html) is not { Success: true } article)
            return "";
        IEnumerable<string> paragraphs = Paragraph().Split(Image().Replace(article.Groups["body"].Value, ""))
            .Select(Html.Text)
            .Where(text => text.Length > 0 && !text.EndsWith(SoundCloudCaption, StringComparison.OrdinalIgnoreCase));
        return string.Join("\n\n", paragraphs);
    }

    private static IReadOnlyList<string> Artwork(string html, string title)
    {
        List<string> found = [];
        if (Article().Match(html) is { Success: true } article)
            found.AddRange(Html.Images(article.Groups["body"].Value).Select(tag => Html.Attribute(tag, "src")).OfType<string>());
        if (SharedImage().Match(html) is { Success: true } shared)
            found.Add(Html.Text(shared.Groups["url"].Value));
        return found.Where(url => url.StartsWith("https://", StringComparison.Ordinal)).Distinct().ToList();
    }

    [GeneratedRegex("""<h1 class="header-heading[^"]*">(?<title>.*?)</h1>""", RegexOptions.Singleline)]
    private static partial Regex Heading();

    [GeneratedRegex("""<div class="header-subheading[^"]*">(?<label>.*?)</div>""", RegexOptions.Singleline)]
    private static partial Regex Subheading();

    [GeneratedRegex("""<div class="d-flex header-content-middle[^"]*">(?<byline>.*?)<div class="mt-auto""", RegexOptions.Singleline)]
    private static partial Regex BylineBlock();

    [GeneratedRegex(">(?<text>[^<]+)<")]
    private static partial Regex TextNode();

    [GeneratedRegex("<img\\s[^>]*>")]
    private static partial Regex Image();

    [GeneratedRegex("""class="content[^"]*\bmedia_(?<id>\d+)\b""")]
    private static partial Regex MediaClass();

    [GeneratedRegex("""<li><a href="/series/(?<slug>[a-z0-9-]+)">(?<name>[^<]*)</a></li>""")]
    private static partial Regex SeriesCrumb();

    [GeneratedRegex(""""<audio src="(?<url>https://[^"]+)"""")]
    private static partial Regex Audio();

    [GeneratedRegex(""""<iframe src="(?<url>[^"]+)"""")]
    private static partial Regex Frame();

    [GeneratedRegex("""<h2 class="h3 my-0 pb-2">Reference</h2>(?<passage>.*?)</div>""", RegexOptions.Singleline)]
    private static partial Regex Reference();

    [GeneratedRegex("""<div class="article mt-3">(?<body>.*?)</div>\s*</div>\s*<div class="col-md-4">""", RegexOptions.Singleline)]
    private static partial Regex Article();

    [GeneratedRegex("</p>|<br\\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex Paragraph();

    [GeneratedRegex(""""<meta property="og:image" content="(?<url>https://[^"]+)"""")]
    private static partial Regex SharedImage();
}
