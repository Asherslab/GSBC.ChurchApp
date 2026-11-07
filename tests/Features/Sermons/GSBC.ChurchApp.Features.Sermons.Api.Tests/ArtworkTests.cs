using GSBC.ChurchApp.Features.Sermons.Api.Data;
using Microsoft.EntityFrameworkCore;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

[Integration("A picture is kept once per content hash, and a known address is only revalidated with the ETag saved for it")]
public sealed class ArtworkTests : SermonsHost
{
    private const string Harbour = "https://files.example/2026/11/01/art/harbour-4.png";

    [Fact]
    public async Task APictureUsedByManySermonsAndSeriesIsKeptOnceAsTheWebsiteHasItAndEveryUserHoldsAReference()
    {
        await ReadTheWebsiteAsync();

        Assert.Equal([(1920, 1080), (300, 400)], Media.Kept.Select(file => (file.Width, file.Height)));
        List<SermonRecord> sermons = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking()
            .Where(sermon => sermon.ArtworkSourceUrl != null).ToListAsync(Cancel));
        List<SeriesRecord> series = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Series.AsNoTracking()
            .Where(record => record.ArtworkSourceUrl != null).ToListAsync(Cancel));
        Assert.True(sermons.Count >= 2 && series.Count >= 2);
        Assert.Single(sermons.Select(sermon => sermon.Artwork!.Key).Concat(series.Select(record => record.Artwork!.Key)).Distinct());
        foreach (SermonRecord sermon in sermons)
            Assert.Equal([sermon.Artwork!.Id], Media.Uses[Artwork.SermonOwner(sermon.Id)]);
        foreach (SeriesRecord record in series)
            Assert.Equal([record.Artwork!.Id], Media.Uses[Artwork.SeriesOwner(record.Id)]);
    }

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
        Assert.Equal(kept + 1, Media.Kept.Count);
        SermonRecord changed = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking()
            .SingleAsync(sermon => sermon.ArtworkSourceUrl == Harbour, Cancel));
        Assert.Equal(changed.ArtworkHash, Artwork.HashOf(Media.Kept[^1].Bytes));
    }
}
