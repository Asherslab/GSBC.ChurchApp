using System.Globalization;
using System.Xml.Linq;

namespace GSBC.ChurchApp.Features.Sermons.Api.Website;

internal sealed record PodcastItem(
    string Guid, string Title, string? Link, DateTime PublishedAt, string Author, string Summary,
    string AudioUrl, string AudioType, long AudioBytes, string? ImageUrl);

internal static class PodcastFeed
{
    private static readonly XNamespace Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

    public static IReadOnlyList<PodcastItem> Read(Stream xml)
    {
        XDocument document = XDocument.Load(xml);
        List<PodcastItem> items = [];
        foreach (XElement item in document.Descendants("item"))
        {
            XElement? enclosure = item.Element("enclosure");
            string? url = enclosure?.Attribute("url")?.Value;
            string? link = Text(item.Element("link"));
            string guid = Text(item.Element("guid")) ?? link ?? url ?? "";
            if (url is null || guid.Length == 0 || !TryDate(Text(item.Element("pubDate")), out DateTime publishedAt))
                continue;

            items.Add(new PodcastItem(
                guid,
                (Text(item.Element("title")) ?? "").Trim(),
                link,
                publishedAt,
                (Text(item.Element(Itunes + "author")) ?? "").Trim(),
                (Text(item.Element(Itunes + "summary")) ?? Text(item.Element("description")) ?? "").Trim(),
                url,
                enclosure!.Attribute("type")?.Value ?? "audio/mpeg",
                long.TryParse(enclosure.Attribute("length")?.Value, out long bytes) ? bytes : 0,
                item.Element(Itunes + "image")?.Attribute("href")?.Value));
        }
        return items;
    }

    private static string? Text(XElement? element) => element is null || element.Value.Length == 0 ? null : element.Value;

    private static bool TryDate(string? text, out DateTime utc)
    {
        utc = default;
        if (text is null || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTimeOffset at))
            return false;
        utc = at.UtcDateTime;
        return true;
    }
}
