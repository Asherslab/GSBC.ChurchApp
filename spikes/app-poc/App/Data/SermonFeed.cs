using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Gsbc.AppPoc.Data;

public static partial class SermonFeed
{
    private static readonly XNamespace Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

    public static IReadOnlyList<Sermon> Parse(string xml) =>
        XDocument.Parse(xml).Descendants("item").Select(ParseItem).OfType<Sermon>().ToList();

    private static Sermon? ParseItem(XElement item)
    {
        var link = (string?)item.Element("link");
        var rawTitle = (string?)item.Element("title");
        if (link is null || rawTitle is null || !TryParseDate((string?)item.Element("pubDate"), out var date))
        {
            return null;
        }

        var (part, title) = SplitTitle(rawTitle.Trim());
        var summary = (string?)item.Element(Itunes + "summary") ?? "";
        var series = ((string?)item.Element(Itunes + "subtitle"))?.Trim();

        return new Sermon(
            Id: link.TrimEnd('/').Split('/')[^1],
            Title: title,
            Part: part,
            Speaker: ((string?)item.Element(Itunes + "author"))?.Trim() ?? "",
            Date: date,
            Service: ServiceName(summary),
            Series: string.IsNullOrEmpty(series) ? null : series,
            YouTubeId: YouTubeId().Match(summary) is { Success: true } m ? m.Groups[1].Value : null,
            AudioUrl: (string?)item.Element("enclosure")?.Attribute("url"),
            ImageUrl: OriginalArtwork((string?)item.Element(Itunes + "image")?.Attribute("href")),
            Link: link);
    }

    private static string? OriginalArtwork(string? podcastImage)
    {
        const string ResizerPath = "/df_media/";
        const string Originals = "https://dq5pwpg1q8ru0.cloudfront.net/";
        var start = podcastImage?.IndexOf(ResizerPath, StringComparison.Ordinal) ?? -1;
        if (start < 0)
        {
            return podcastImage;
        }
        var job = podcastImage![(start + ResizerPath.Length)..].Split('/')[0];
        try
        {
            var padded = job.Replace('-', '+').Replace('_', '/').PadRight(job.Length + (4 - job.Length % 4) % 4, '=');
            using var steps = JsonDocument.Parse(Convert.FromBase64String(padded));
            var file = steps.RootElement.EnumerateArray().FirstOrDefault(step => step[0].GetString() == "f");
            return file.ValueKind == JsonValueKind.Array ? Originals + file[1].GetString() : podcastImage;
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return podcastImage;
        }
    }

    private static (string? Part, string Title) SplitTitle(string raw)
    {
        var dash = raw.IndexOf(" - ", StringComparison.Ordinal);
        return dash > 0 && raw[..dash].Contains("Part", StringComparison.OrdinalIgnoreCase)
            ? (raw[..dash], raw[(dash + 3)..])
            : (null, raw);
    }

    private static string ServiceName(string summary)
    {
        var tag = summary.Split(" - ")[0].Trim();
        return tag switch
        {
            _ when tag.StartsWith("Sun", StringComparison.OrdinalIgnoreCase) && tag.EndsWith("PM", StringComparison.OrdinalIgnoreCase) => "Sunday PM",
            _ when tag.StartsWith("Sun", StringComparison.OrdinalIgnoreCase) => "Sunday AM",
            _ when tag.StartsWith("Wed", StringComparison.OrdinalIgnoreCase) => "Wednesday",
            _ => "",
        };
    }

    private static bool TryParseDate(string? pubDate, out DateOnly date)
    {
        date = default;
        var parts = pubDate?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts is { Length: >= 4 }
            && DateOnly.TryParseExact($"{parts[1]} {parts[2]} {parts[3]}", "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    [GeneratedRegex(@"(?:youtu\.be/|[?&]v=|/live/|/embed/)([A-Za-z0-9_-]{11})")]
    private static partial Regex YouTubeId();
}
