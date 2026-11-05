using System.Net;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class WebsiteReadingTests : SermonsHost
{
    [Fact]
    public async Task ReadingTheWebsiteFillsTheCatalogueWithItsSeriesSpeakersServicesAndVideos()
    {
        await ReadTheWebsiteAsync();

        List<Sermon> catalogue = await CatalogueAsync();
        Assert.Equal(4, catalogue.Count);

        Sermon hope = catalogue.Single(sermon => sermon.Title.StartsWith("Safe Harbour: Part 4", StringComparison.Ordinal));
        Assert.Equal("Safe Harbour", hope.SeriesName);
        Assert.Equal(4, hope.Part);
        Assert.Equal("Sunday morning", hope.Service);
        Assert.Equal("AbCdEfGhIj0", hope.YouTubeId);
        Assert.Equal("Jo Ellery", hope.SpeakerName);
        Assert.Equal((1920, 1080), (hope.Artwork!.Width, hope.Artwork.Height));
        Assert.Contains(Website.Requests, request => request.RequestUri!.ToString() == "https://files.example/2026/11/01/art/harbour-4.png");

        Sermon mercy = catalogue.Single(sermon => sermon.Title == "Mercy Before Judgement");
        Assert.Equal("Fruit Of The Spirit", mercy.SeriesName);
        Assert.Equal("Sunday evening", mercy.Service);
        Assert.Equal("Zz9Yy8Xx7Ww", mercy.YouTubeId);
        Assert.NotNull(mercy.Artwork);

        Sermon walking = catalogue.Single(sermon => sermon.Title == "Walking In The Light");
        Assert.Null(walking.SeriesId);
        Assert.Equal("Wednesday", walking.Service);
        Assert.Equal(1000, walking.DurationSeconds);
        Assert.Null(walking.YouTubeId);

        List<SeriesInfo> series = (await LiveRowsAsync(SermonsFeeds.Series)).Select(row => SermonsPayloads.Decode<SeriesInfo>(row.Payload)).ToList();
        Assert.Equal(["Fruit Of The Spirit", "Safe Harbour"], series.Select(info => info.Name).Order());
        Assert.Equal(2, series.Single(info => info.Name == "Safe Harbour").Count);

        List<SpeakerInfo> speakers = (await LiveRowsAsync(SermonsFeeds.Speakers)).Select(row => SermonsPayloads.Decode<SpeakerInfo>(row.Payload)).ToList();
        Assert.Equal((300, 400), speakers.Single(speaker => speaker.Name == "Jo Ellery").Photo is { } photo ? (photo.Width, photo.Height) : default);
    }

    [Fact]
    public async Task StaffCorrectionsSurviveTheNextReadAndCanBeUndone()
    {
        await ReadTheWebsiteAsync();
        Sermon mercy = (await CatalogueAsync()).Single(sermon => sermon.Title == "Mercy Before Judgement");

        await WithAsync(provider => provider.GetRequiredService<SermonLibraryService>().CorrectAsync(new SermonCorrection
        {
            Id = mercy.Id, Title = "Mercy Comes First", SeriesId = "", Service = "Sunday night",
        }).AsTask());

        Website.FeedETag = "W/\"two\"";
        Website.ServeFeed(Sample("feed.rss").Replace("<title>Mercy Before Judgement</title>", "<title>Mercy Before Judgment</title>"));
        await ReadTheWebsiteAsync();

        Sermon corrected = (await CatalogueAsync()).Single(sermon => sermon.Id == mercy.Id);
        Assert.Equal(("Mercy Comes First", null, "Sunday night"), (corrected.Title, corrected.SeriesName, corrected.Service));
        SermonDetail detail = await DetailAsync(mercy.Id);
        Assert.Equal("Mercy Before Judgment", detail.FromSource.Title);
        Assert.Equal(["title", "series", "service"], detail.Corrected);

        await WithAsync(provider => provider.GetRequiredService<SermonLibraryService>().CorrectAsync(new SermonCorrection
        {
            Id = mercy.Id, UseSource = ["title", "series"],
        }).AsTask());
        Sermon undone = (await CatalogueAsync()).Single(sermon => sermon.Id == mercy.Id);
        Assert.Equal(("Mercy Before Judgment", "Fruit Of The Spirit", "Sunday night"), (undone.Title, undone.SeriesName, undone.Service));
    }

    [Fact]
    public async Task ASermonTheWebsiteDropsStaysAndAnUnchangedFeedIsNotReadAgain()
    {
        await ReadTheWebsiteAsync();
        string trimmed = Sample("feed.rss");
        trimmed = trimmed[..trimmed.IndexOf("    <item>\n      <title>Safe Harbour: Part 3", StringComparison.Ordinal)] + "  </channel>\n</rss>\n";

        Website.ServeFeed(trimmed);
        Website.Requests.Clear();
        await ReadTheWebsiteAsync();
        HttpRequestMessage feedRequest = Website.Requests.First(request => request.RequestUri!.AbsolutePath.EndsWith(".rss", StringComparison.Ordinal));
        Assert.Contains(feedRequest.Headers.IfNoneMatch, tag => tag.ToString() == "W/\"one\"");

        Website.FeedETag = "W/\"two\"";
        await ReadTheWebsiteAsync();

        Assert.Equal(4, (await CatalogueAsync()).Count);
        Assert.Contains(await CatalogueAsync(), sermon => sermon.Title == "Safe Harbour: Part 3 - Harbour Of Rest");
    }

    [Fact]
    public async Task SeriesPagesThatCantBeReadKeepTheSeriesAsTheyWere()
    {
        await ReadTheWebsiteAsync();
        Website.Fail($"{FakeWebsite.BaseUrl}/podcasts/media/series/safe-harbour", HttpStatusCode.InternalServerError);

        await ReadTheWebsiteAsync(force: true);

        Assert.Equal(2, (await CatalogueAsync()).Count(sermon => sermon.SeriesName == "Safe Harbour"));
        SourceStatus status = await WithAsync(provider => provider.GetRequiredService<SermonLibraryService>().StatusAsync(new Done()).AsTask());
        Assert.Equal("The website answered 500.", status.Problem);
    }

    [Fact]
    public async Task PhonesHoldTheNewestSermonsAndOneLeavingTheWindowIsSentAsADelete()
    {
        DateTime first = new(2020, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        await WithAsync(async provider =>
        {
            SermonsDbContext db = provider.GetRequiredService<SermonsDbContext>();
            for (int week = 0; week < SermonsFeeds.CatalogueWindow; week++)
                db.Sermons.Add(Uploaded(first.AddDays(7 * week), $"Week {week}"));
            await db.SaveChangesAsync(Cancel);
            await provider.GetRequiredService<SermonPublisher>().PublishAsync(Cancel);
        });
        Assert.Equal(SermonsFeeds.CatalogueWindow, (await CatalogueAsync()).Count);

        await WithAsync(async provider =>
        {
            SermonsDbContext db = provider.GetRequiredService<SermonsDbContext>();
            db.Sermons.Add(Uploaded(first.AddDays(7 * SermonsFeeds.CatalogueWindow), "The newest"));
            await db.SaveChangesAsync(Cancel);
            await provider.GetRequiredService<SermonPublisher>().PublishAsync(Cancel);
        });

        List<Sermon> catalogue = await CatalogueAsync();
        Assert.Equal(SermonsFeeds.CatalogueWindow, catalogue.Count);
        Assert.Contains(catalogue, sermon => sermon.Title == "The newest");
        Assert.DoesNotContain(catalogue, sermon => sermon.Title == "Week 0");
        Assert.True(await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().FeedChanges
            .AnyAsync(change => change.Feed == SermonsFeeds.Catalogue && change.Deleted, Cancel)));
    }

    [Fact]
    public async Task TheFirstReadIsTakenAsCheckedAndANewSermonNeedsALookUntilItLooksRight()
    {
        string trimmed = Sample("feed.rss");
        trimmed = trimmed[..trimmed.IndexOf("    <item>\n      <title>Walking In The Light", StringComparison.Ordinal)]
            + trimmed[trimmed.IndexOf("    <item>\n      <title>Safe Harbour: Part 4", StringComparison.Ordinal)..];
        Website.ServeFeed(trimmed);
        await ReadTheWebsiteAsync();
        Assert.Equal(0, (await QueueAsync()).Total);

        Website.FeedETag = "W/\"two\"";
        ServeTheSampleSite();
        Website.FeedETag = "W/\"two\"";
        Website.ServeFeed(Sample("feed.rss"));
        await ReadTheWebsiteAsync();

        SermonList queue = await QueueAsync();
        SermonDetail walking = Assert.Single(queue.Sermons);
        Assert.Equal("Walking In The Light", walking.Sermon.Title);
        Assert.Equal(["New speaker"], walking.NeedsALook);

        await WithAsync(provider => provider.GetRequiredService<SermonLibraryService>().CorrectAsync(new SermonCorrection { Id = walking.Sermon.Id, Checked = true }).AsTask());
        Assert.Equal(0, (await QueueAsync()).Total);
    }

    private Task<SermonList> QueueAsync() => WithAsync(provider =>
        provider.GetRequiredService<SermonLibraryService>().ListAsync(new SermonQuery { Filter = SermonLibraryService.Filters.NeedsALook }).AsTask());

    private Task<SermonDetail> DetailAsync(string id) => WithAsync(async provider =>
        (await provider.GetRequiredService<SermonLibraryService>().ListAsync(new SermonQuery { Take = 200 })).Sermons.Single(detail => detail.Sermon.Id == id));

    private static SermonRecord Uploaded(DateTime preachedAt, string title) => new()
    {
        Id = Guid.CreateVersion7(),
        Origin = SermonSources.Uploads,
        PreachedAt = preachedAt,
        AddedAt = preachedAt,
        SourceTitle = title,
        AudioUrl = $"https://media.example/sermons/{title}.mp3",
    };
}
