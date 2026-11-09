using System.Xml.Linq;
using GSBC.ChurchApp.Features.Sermons.Api.Data;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class PodcastFeedTests
{
    private static readonly XNamespace Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

    [Fact]
    public void EachSermonIsAnEpisodeWithItsSpeakerLengthAndOwnId()
    {
        SpeakerRecord ruth = new() { Id = Guid.CreateVersion7(), Name = "Ruth Calder" };
        SermonRecord soil = new()
        {
            Id = Guid.CreateVersion7(), PreachedAt = new DateTime(2026, 11, 7, 14, 0, 0, DateTimeKind.Utc),
            Title = "The Good Soil", SpeakerId = ruth.Id, AudioUrl = "https://media.example/sermons/audio/good-soil.mp3", DurationSeconds = 1000,
        };
        SermonRecord mercy = new()
        {
            Id = Guid.CreateVersion7(), PreachedAt = new DateTime(2026, 10, 31, 14, 0, 0, DateTimeKind.Utc), Title = "Mercy Before Judgement",
            AudioUrl = "https://media.example/0a1b.mp3",
        };

        List<XElement> items = PodcastFeedWriter.Feed(new PodcastOptions(), "https://media.example/podcast/feed.xml", [soil, mercy],
            new Dictionary<Guid, SeriesRecord>(), new Dictionary<Guid, SpeakerRecord> { [ruth.Id] = ruth }).Descendants("item").ToList();

        Assert.Equal(["The Good Soil", "Mercy Before Judgement"], items.Select(item => item.Element("title")!.Value));
        Assert.Equal(("Ruth Calder", "1000"), (items[0].Element(Itunes + "author")!.Value, items[0].Element(Itunes + "duration")!.Value));
        Assert.Equal(SermonPublisher.Unknown, items[1].Element(Itunes + "author")!.Value);
        Assert.Equal((soil.Id.ToString(), "false"), (items[0].Element("guid")!.Value, items[0].Element("guid")!.Attribute("isPermaLink")!.Value));
    }
}
