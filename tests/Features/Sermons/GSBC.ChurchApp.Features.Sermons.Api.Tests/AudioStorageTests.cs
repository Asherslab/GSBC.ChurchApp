using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class AudioStorageTests : SermonsHost
{
    [Fact]
    public async Task CopiedAudioIsStoredOnceByItsContentAndEverySermonHoldsItInMedia()
    {
        await ReadTheWebsiteAsync();
        await WithAsync(provider => provider.GetRequiredService<SermonLibraryService>().ChooseSourceAsync(new SourceChoice { Source = SermonSources.Uploads }).AsTask());

        await WithAsync(provider => provider.GetRequiredService<AudioCopier>().RunAsync(Cancel));

        List<SermonRecord> sermons = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking().ToListAsync(Cancel));
        Assert.All(sermons, sermon => Assert.NotNull(sermon.AudioCopiedFrom));
        Assert.Single(sermons.Select(sermon => sermon.AudioUrl).Distinct());
        Assert.Single(Blobs.Objects.Keys, stored => stored.Item2.EndsWith(".mp3", StringComparison.Ordinal));
        Assert.Single(Media.Stored);
        Assert.All(sermons, sermon => Assert.Equal([sermon.AudioMedia!.Id], Media.Uses[AudioCopier.AudioOwner(sermon.Id)]));

        CopyProgress progress = await WithAsync(provider => provider.GetRequiredService<SermonLibraryService>().CopyingAsync(new Done()).AsTask());
        Assert.Equal((sermons.Count, sermons.Count), (progress.Copied, progress.Total));
        Assert.All(progress.Items, item => Assert.Equal(CopyStates.Copied, item.State));
    }
}
