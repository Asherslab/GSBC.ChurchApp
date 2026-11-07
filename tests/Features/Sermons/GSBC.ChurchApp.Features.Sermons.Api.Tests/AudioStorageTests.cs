using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using Microsoft.EntityFrameworkCore;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

[Integration("Copied audio is stored once under its content hash and every sermon holds that one file in Media")]
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
    }
}
