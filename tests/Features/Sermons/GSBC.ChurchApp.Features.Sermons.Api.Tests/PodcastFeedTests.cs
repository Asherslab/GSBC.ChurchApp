using System.Xml.Linq;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class PodcastFeedTests
{
    private static readonly XNamespace Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

    [Fact]
    public void ThePodcastFeedPutsAnUploadedSermonFirstAndKeepsTheWebsitesEpisodesAsTheyWere()
    {
        SpeakerRecord ruth = new() { Id = Guid.CreateVersion7(), Name = "Ruth Calder" };
        SermonRecord uploaded = new()
        {
            Id = Guid.CreateVersion7(), Origin = SermonSources.Uploads, PreachedAt = new DateTime(2026, 11, 7, 14, 0, 0, DateTimeKind.Utc),
            SourceTitle = "The Good Soil", SourceSpeakerId = ruth.Id, AudioUrl = "https://media.example/sermons/audio/good-soil.mp3", DurationSeconds = 1000,
        };
        SermonRecord fromWebsite = new()
        {
            Id = Guid.CreateVersion7(), Origin = SermonSources.Website, ExternalId = "https://church.example/media/2026-11-01-mercy-before-judgement",
            PreachedAt = new DateTime(2026, 10, 31, 14, 0, 0, DateTimeKind.Utc), SourceTitle = "Mercy Before Judgement", SourceSpeaker = "Lee Varga",
            AudioUrl = "https://media.example/0a1b.mp3",
        };

        List<XElement> items = PodcastFeedWriter.Feed(new PodcastOptions(), "https://media.example/podcast/feed.xml", [uploaded, fromWebsite],
            new Dictionary<Guid, SeriesRecord>(), new Dictionary<Guid, SpeakerRecord> { [ruth.Id] = ruth }).Descendants("item").ToList();

        Assert.Equal(["The Good Soil", "Mercy Before Judgement"], items.Select(item => item.Element("title")!.Value));
        Assert.Equal(("Ruth Calder", "1000"), (items[0].Element(Itunes + "author")!.Value, items[0].Element(Itunes + "duration")!.Value));
        Assert.Equal((fromWebsite.ExternalId, "true"), (items[1].Element("guid")!.Value, items[1].Element("guid")!.Attribute("isPermaLink")!.Value));
        Assert.Equal((uploaded.Id.ToString(), "false"), (items[0].Element("guid")!.Value, items[0].Element("guid")!.Attribute("isPermaLink")!.Value));
    }
}
