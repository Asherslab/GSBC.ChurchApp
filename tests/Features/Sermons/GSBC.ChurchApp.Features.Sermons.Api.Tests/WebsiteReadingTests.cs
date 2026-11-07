using System.Net;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

[Integration("Each read builds on what earlier reads saved: the feed's ETag, the sermons and series, and which sermons were checked and announced")]
public sealed class WebsiteReadingTests : SermonsHost
{
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
    public async Task TheFirstReadIsTakenAsCheckedAndAnnouncedAndALaterNewSermonWaitsForALookAndIsAnnouncedOnce()
    {
        string first = Sample("feed.rss");
        Website.ServeFeed(first[..first.IndexOf("    <item>\n      <title>Walking In The Light", StringComparison.Ordinal)]
            + first[first.IndexOf("    <item>\n      <title>Safe Harbour: Part 4", StringComparison.Ordinal)..]);
        await ReadTheWebsiteAsync();
        Assert.Equal(0, (await QueueAsync()).Total);
        Assert.Empty(Notifications.Raised);

        Website.FeedETag = "W/\"two\"";
        Website.ServeFeed(first);
        await ReadTheWebsiteAsync();
        await ReadTheWebsiteAsync(force: true);

        SermonDetail walking = Assert.Single((await QueueAsync()).Sermons);
        Assert.Equal("Walking In The Light", walking.Sermon.Title);
        Assert.Equal($"sermon/{walking.Sermon.Id}", Assert.Single(Notifications.Raised).Subject);

        await WithAsync(provider => provider.GetRequiredService<SermonLibraryService>().CorrectAsync(new SermonCorrection { Id = walking.Sermon.Id, Checked = true }).AsTask());
        Assert.Equal(0, (await QueueAsync()).Total);
    }

    private Task<SermonList> QueueAsync() => WithAsync(provider =>
        provider.GetRequiredService<SermonLibraryService>().ListAsync(new SermonQuery { Filter = SermonLibraryService.Filters.NeedsALook }).AsTask());
}
