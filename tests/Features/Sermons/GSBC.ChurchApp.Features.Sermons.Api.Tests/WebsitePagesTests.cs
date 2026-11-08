using System.Net;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Api.Website;
using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class WebsitePagesTests
{
    private const string FileHost = "files.example";
    private static readonly DateTime Now = new(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TheWebsitesFeedAndSeriesPagesGiveEachSermonItsServiceVideoPictureSeriesAndSpeaker()
    {
        Dictionary<string, SermonRecord> sermons = FeedItems().ToDictionary(item => item.Title, Read);
        Assert.Equal(4, sermons.Count);

        SermonRecord hope = sermons["Safe Harbour: Part 4 - Harbour Of Hope"];
        Assert.Equal(("Sunday morning", "AbCdEfGhIj0", "Jo Ellery"), (hope.SourceService, hope.SourceYouTubeId, hope.SourceSpeaker));
        Assert.Equal("https://files.example/2026/11/01/art/harbour-4.png", hope.ArtworkSourceUrl);
        Assert.Equal(4, TitleParts.PartOf(hope.EffectiveTitle));

        SermonRecord mercy = sermons["Mercy Before Judgement"];
        Assert.Equal(("Sunday evening", "Zz9Yy8Xx7Ww"), (mercy.SourceService, mercy.SourceYouTubeId));

        SermonRecord walking = sermons["Walking In The Light"];
        Assert.Equal(("Wednesday", null), (walking.SourceService, walking.SourceYouTubeId));
        Assert.Equal(1000, Mp3Duration.Seconds(SampleFiles.ConstantBitrateMp3(), walking.AudioBytes));

        Assert.Equal(["Safe Harbour", "Fruit Of The Spirit", "Older Days & Ways"], SeriesPages.ReadIndex(SampleFiles.Text("media.html")).Select(link => link.Name));
        SeriesPage harbour = SeriesPages.ReadSeries(SampleFiles.Text("series-safe-harbour.html"), "Safe Harbour");
        Assert.Equal("https://files.example/2026/10/series/safe-harbour.png", harbour.ArtworkUrl);
        Assert.Contains(new Uri(hope.WebLink!).AbsolutePath, harbour.SermonPaths);
        Assert.Equal("https://files.example/people/ellery.jpg", harbour.SpeakerPhotos["Jo Ellery"]);
        SeriesPage fruit = SeriesPages.ReadSeries(SampleFiles.Text("series-fruit-of-the-spirit.html"), "Fruit Of The Spirit");
        Assert.Contains(new Uri(mercy.WebLink!).AbsolutePath, fruit.SermonPaths);
    }

    [Fact]
    public void TheTeamPageGivesOnePhotoForEachPersonAndLeavesOutCouplesAndNamesWithTwoPhotos()
    {
        TeamPhotos team = TeamPage.Read(SampleFiles.Text("team.html"));

        Assert.Equal(["Ari Moss", "Jo Ellery", "Pat Quinn"], team.ByName.Keys.Order());
        Assert.Equal("https://files.example/team/Ellery-J_SQUARE.jpg", team.ByName["Jo Ellery"]);
        Assert.Equal(["Jo & Kim Ellery", "Sam Rivers"], team.LeftOut);
    }

    [Fact]
    public void ATeamPhotoWinsOverASeriesPagePhotoOfTheSameSpeakerAndTheSeriesOneIsTheFallback()
    {
        IReadOnlyDictionary<string, string> photos = Speakers.Preferring(
            new Dictionary<string, string> { ["Jo  Ellery"] = "team/jo.jpg" },
            new Dictionary<string, string> { ["Jo Ellery"] = "series/jo.jpg", ["Lee Varga"] = "series/lee.jpg" });

        Assert.Equal(new Dictionary<string, string> { ["jo ellery"] = "team/jo.jpg", ["lee varga"] = "series/lee.jpg" }, photos);
    }

    [Fact]
    public void ASermonShowsTheWholeGraphicFromItsPageRatherThanTheFeedsSquareCrop()
    {
        const string Whole = "https://files.example/2026/11/01/art/harbour-4-wide.png";
        const string Smaller = "https://files.example/2026/11/01/art/harbour-4-page.png";
        string page = $$"""<html><head><meta property="og:image" content="https://church.example/df_media/{{DragonflyJob("2026/11/01/art/harbour-4-wide.png")}}/harbour-4-wide.png?sha=1" /></head><body><img class="mb-3" alt="Safe Harbour: Part 4 - Harbour of Hope" src="{{Smaller}}" /></body></html>""";
        Dictionary<string, byte[]> pictures = new() { [Whole] = SampleFiles.Picture(1672, 941), [Smaller] = SampleFiles.Picture(800, 450) };

        IReadOnlyList<string> offered = SermonPages.ArtworkOf(page, "Safe Harbour: Part 4 - Harbour Of Hope", FileHost);

        Assert.Equal([Whole, Smaller], offered);
        Assert.Equal(Whole, Artwork.Largest(offered.Select(url => (url, pictures[url]))));
    }

    [Fact]
    public void ASermonsPageIsReadAgainOnlyWhenTheFeedOffersADifferentPicture()
    {
        PodcastItem hope = FeedItems().Single(item => item.Title.StartsWith("Safe Harbour: Part 4", StringComparison.Ordinal));
        SermonRecord sermon = Read(hope);
        sermon.PageReadAt = Now;
        sermon.ArtworkSourceUrl = "https://files.example/2026/11/01/art/harbour-4-wide.png";

        WebsiteReader.Take(sermon, hope, null, FileHost, Now);
        Assert.Equal((Now, "https://files.example/2026/11/01/art/harbour-4-wide.png"), (sermon.PageReadAt, sermon.ArtworkSourceUrl));

        WebsiteReader.Take(sermon, hope with { ImageUrl = $"https://church.example/df_media/{DragonflyJob("2026/11/08/art/harbour-5.png")}/harbour-5.jpg" }, null, FileHost, Now);
        Assert.Null(sermon.PageReadAt);
        Assert.Equal("https://files.example/2026/11/08/art/harbour-5.png", sermon.ArtworkSourceUrl);
    }

    [Fact]
    public void ASeriesStaffRenamedKeepsTheirNameAndOneTheyLeftAloneFollowsTheWebsite()
    {
        SeriesPage page = new(null, [], new Dictionary<string, string>(), []);
        SeriesRecord renamed = new() { Name = "Safe Harbour" };
        SeriesRecord untouched = new() { Name = "Fruit Of The Spirit" };
        WebsiteReader.Take(renamed, new SeriesLink("safe-harbour", "Safe Harbour"), page);
        WebsiteReader.Take(untouched, new SeriesLink("fruit-of-the-spirit", "Fruit Of The Spirit"), page);

        renamed.Name = "Harbours";
        WebsiteReader.Take(renamed, new SeriesLink("safe-harbour", "Safe Harbour (2026)"), page);
        WebsiteReader.Take(untouched, new SeriesLink("fruit-of-the-spirit", "The Fruit Of The Spirit"), page);

        Assert.Equal(("Harbours", "The Fruit Of The Spirit"), (renamed.Name, untouched.Name));
    }

    [Fact]
    public void AWebsiteProblemIsToldInPlainWords()
    {
        Assert.Equal("The website answered 500.", WebsiteReader.Problem(new HttpRequestException(null, null, HttpStatusCode.InternalServerError)));
        Assert.Equal("The website couldn't be reached.", WebsiteReader.Problem(new HttpRequestException()));
    }

    [Theory]
    [InlineData("https://files.example/2026/11/01/art/harbour-4.png", "harbour-4.png")]
    [InlineData("https://files.example/2026/11/01/art/%3Cscript%3E.png", "0123456789ab.png")]
    public void AKeptPictureIsNamedAsTheWebsiteNamesItOrByItsHash(string url, string name) =>
        Assert.Equal(name, Artwork.FileNameOf(url, "0123456789abcdef", "png"));

    private static IReadOnlyList<PodcastItem> FeedItems()
    {
        using MemoryStream feed = new(System.Text.Encoding.UTF8.GetBytes(SampleFiles.Text("feed.rss")));
        return PodcastFeed.Read(feed);
    }

    private static SermonRecord Read(PodcastItem item)
    {
        SermonRecord sermon = new() { Origin = SermonSources.Website, ExternalId = item.Guid };
        WebsiteReader.Take(sermon, item, null, FileHost, Now);
        return sermon;
    }

    private static string DragonflyJob(string path) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"[[\"f\",\"{path}\"],[\"p\",\"thumb\",\"690x\"]]")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
