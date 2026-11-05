using GSBC.ChurchApp.Features.Sermons.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class ArtworkTests : SermonsHost
{
    private const string Harbour = "https://files.example/2026/11/01/art/harbour-4.png";

    [Fact]
    public async Task APictureUsedByManySermonsAndSeriesIsMadeAndKeptOnceAndEveryUserHoldsAReference()
    {
        await ReadTheWebsiteAsync();

        Assert.Equal(["large", "small", "photo"], Media.Kept.Select(file => file.FileName.Split('-', '.')[1]));
        List<SermonRecord> sermons = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking()
            .Where(sermon => sermon.ArtworkSourceUrl != null).ToListAsync(Cancel));
        List<SeriesRecord> series = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Series.AsNoTracking()
            .Where(record => record.ArtworkSourceUrl != null).ToListAsync(Cancel));
        Assert.True(sermons.Count >= 2 && series.Count >= 2);
        Assert.Single(sermons.Select(sermon => sermon.Artwork!.Key).Concat(series.Select(record => record.Artwork!.Key)).Distinct());
        foreach (SermonRecord sermon in sermons)
            Assert.Equal([sermon.Artwork!.Id, sermon.Thumbnail!.Id], Media.Uses[Artwork.SermonOwner(sermon.Id)]);
        foreach (SeriesRecord record in series)
            Assert.Equal([record.Artwork!.Id, record.Thumbnail!.Id], Media.Uses[Artwork.SeriesOwner(record.Id)]);
    }

    [Fact]
    public async Task ASermonShowsTheWholeGraphicFromItsPageRatherThanTheFeedsSquareCrop()
    {
        const string Page = "https://church.example/media/2026-11-01-safe-harbour-part-4-harbour-of-hope";
        const string Whole = "https://files.example/2026/11/01/art/harbour-4-wide.png";
        const string Smaller = "https://files.example/2026/11/01/art/harbour-4-page.png";
        Website.ServeText(Page, $$"""<html><head><meta property="og:image" content="https://church.example/df_media/{{DragonflyJob("2026/11/01/art/harbour-4-wide.png")}}/harbour-4-wide.png?sha=1" /></head><body><img class="mb-3" alt="Safe Harbour: Part 4 - Harbour of Hope" src="{{Smaller}}" /></body></html>""");
        Website.ServeBytes(Whole, Picture(1672, 941));
        Website.ServeBytes(Smaller, Picture(800, 450));

        await ReadTheWebsiteAsync();

        SermonRecord sermon = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking()
            .SingleAsync(record => record.WebLink == Page, Cancel));
        Assert.Equal(Whole, sermon.ArtworkSourceUrl);
        Assert.Equal(Harbour, sermon.FeedArtworkUrl);
        Assert.Equal((1280, 720), (sermon.Artwork!.Width, sermon.Artwork.Height));

        Website.Requests.Clear();
        await ReadTheWebsiteAsync(force: true);
        Assert.DoesNotContain(Website.Requests, request => request.RequestUri!.ToString() == Page);
    }

    private static string DragonflyJob(string path) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"[[\"f\",\"{path}\"],[\"p\",\"thumb\",\"690x\"]]")).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [Fact]
    public async Task AKnownPictureIsOnlyRevalidatedAndIsMadeAgainOnlyWhenItChanges()
    {
        Website.ServeBytes(Harbour, Picture(1920, 1080), "\"one\"");
        await ReadTheWebsiteAsync();
        int kept = Media.Kept.Count;

        await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons
            .ExecuteUpdateAsync(set => set.SetProperty(sermon => sermon.ArtworkHash, (string?)null), Cancel));
        Website.Requests.Clear();
        await ReadTheWebsiteAsync(force: true);
        Assert.Equal(kept, Media.Kept.Count);
        Assert.Contains(Website.Requests, request => request.RequestUri!.ToString() == Harbour && request.Headers.IfNoneMatch.Count == 1);

        Website.ServeBytes(Harbour, Picture(1920, 1080, 0xFF8A5A2B), "\"two\"");
        await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons
            .ExecuteUpdateAsync(set => set.SetProperty(sermon => sermon.ArtworkHash, (string?)null), Cancel));
        await ReadTheWebsiteAsync(force: true);
        Assert.Equal(kept + 2, Media.Kept.Count);
        SermonRecord changed = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking()
            .SingleAsync(sermon => sermon.ArtworkSourceUrl == Harbour, Cancel));
        Assert.Equal(Media.Kept[^2].FileName.Split('-')[0], changed.ArtworkHash![..12]);
    }
}
