using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

[Integration("Publishing writes a delete row to the catalogue feed when a sermon leaves the window phones hold")]
public sealed class CatalogueWindowTests : SermonsHost
{
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
