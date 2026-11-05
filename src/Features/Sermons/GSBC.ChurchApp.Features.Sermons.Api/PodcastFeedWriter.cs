using System.Globalization;
using System.Text;
using System.Xml.Linq;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Sermons.Api;

public sealed class PodcastOptions
{
    public const string Section = "Sermons:Podcast";

    public string Title { get; set; } = "Good Shepherd Baptist Church";
    public string Description { get; set; } = "Sermons from Good Shepherd Baptist Church, Albany Creek";
    public string Link { get; set; } = "https://baptist.com.au";
    public string Owner { get; set; } = "Good Shepherd Baptist Church";
    public string? Email { get; set; }
    public string? ImageUrl { get; set; }
}

internal sealed class PodcastFeedWriter(SermonsDbContext db, IBlobStore blobs, IOptions<PodcastOptions> options)
{
    public const string Key = "podcast/feed.xml";
    public const string CacheControl = "public, max-age=300";
    private static readonly XNamespace Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";

    public string Url => blobs.PublicUrlOf(Key).ToString();

    public async Task WriteAsync(CancellationToken cancellationToken)
    {
        List<SermonRecord> sermons = await db.Sermons.AsNoTracking().Where(sermon => !sermon.Hidden && sermon.AudioUrl != "")
            .OrderByDescending(sermon => sermon.PreachedAt).ThenByDescending(sermon => sermon.AddedAt).ToListAsync(cancellationToken);
        Dictionary<Guid, SpeakerRecord> speakers = await db.Speakers.AsNoTracking().ToDictionaryAsync(speaker => speaker.Id, cancellationToken);
        Dictionary<Guid, SeriesRecord> series = await db.Series.AsNoTracking().ToDictionaryAsync(record => record.Id, cancellationToken);

        XDocument feed = Feed(sermons, series, speakers);
        byte[] xml = Encoding.UTF8.GetBytes(feed.Declaration + Environment.NewLine + feed.Root);
        await blobs.PutAsync(BlobBucket.Public, Key, new MemoryStream(xml), new BlobHeaders("application/rss+xml; charset=utf-8", CacheControl), cancellationToken);
    }

    internal XDocument Feed(IReadOnlyList<SermonRecord> sermons, IReadOnlyDictionary<Guid, SeriesRecord> series, IReadOnlyDictionary<Guid, SpeakerRecord> speakers)
    {
        PodcastOptions podcast = options.Value;
        XElement channel = new("channel",
            new XElement("title", podcast.Title),
            new XElement("link", podcast.Link),
            new XElement("description", podcast.Description),
            new XElement("language", "en-au"),
            new XElement(Atom + "link", new XAttribute("href", Url), new XAttribute("rel", "self"), new XAttribute("type", "application/rss+xml")),
            new XElement(Itunes + "author", podcast.Owner),
            new XElement(Itunes + "summary", podcast.Description),
            new XElement(Itunes + "owner", new XElement(Itunes + "name", podcast.Owner), podcast.Email is { } email ? new XElement(Itunes + "email", email) : null),
            podcast.ImageUrl is { } image ? new XElement(Itunes + "image", new XAttribute("href", image)) : null,
            new XElement(Itunes + "category", new XAttribute("text", "Religion & Spirituality"), new XElement(Itunes + "category", new XAttribute("text", "Christianity"))),
            new XElement(Itunes + "explicit", "false"));

        foreach (SermonRecord sermon in sermons)
        {
            Sermon payload = SermonPublisher.Payload(sermon, series, speakers);
            string summary = string.Join(" - ", new[] { payload.Service, payload.YouTubeId is { } video ? $"Watch at: {YouTubeIds.WatchUrl(video)}" : null }
                .Where(part => !string.IsNullOrEmpty(part)));
            channel.Add(new XElement("item",
                new XElement("title", payload.Title),
                new XElement("guid", new XAttribute("isPermaLink", sermon.ExternalId is not null && sermon.ExternalId.StartsWith("http", StringComparison.Ordinal) ? "true" : "false"),
                    sermon.ExternalId ?? sermon.Id.ToString()),
                sermon.WebLink is { } link ? new XElement("link", link) : null,
                new XElement("enclosure", new XAttribute("url", sermon.AudioUrl), new XAttribute("length", sermon.AudioBytes), new XAttribute("type", sermon.AudioType)),
                new XElement("pubDate", sermon.PreachedAt.ToString("r", CultureInfo.InvariantCulture)),
                new XElement(Itunes + "author", payload.SpeakerName),
                new XElement(Itunes + "summary", summary),
                new XElement("description", summary),
                sermon.DurationSeconds > 0 ? new XElement(Itunes + "duration", sermon.DurationSeconds) : null,
                payload.Artwork?.Url is { } artwork ? new XElement(Itunes + "image", new XAttribute("href", artwork)) : null,
                new XElement(Itunes + "explicit", "false")));
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement("rss", new XAttribute("version", "2.0"),
                new XAttribute(XNamespace.Xmlns + "itunes", Itunes), new XAttribute(XNamespace.Xmlns + "atom", Atom), channel));
    }
}
