using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Platform.Storage;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

[Integration("Uploaded audio is kept once by its content hash: a second upload of the same recording points at the first file and its own copy is deleted")]
public sealed class AudioStorageTests : SermonsHost
{
    [Fact]
    public async Task TheSameRecordingUploadedTwiceIsStoredOnceAndBothSermonsHoldItInMedia()
    {
        await SeedLibraryAsync();
        byte[] recording = SampleFiles.ConstantBitrateMp3();
        List<SermonRecord> sermons = await WithAsync(async provider =>
        {
            SermonsDbContext db = provider.GetRequiredService<SermonsDbContext>();
            List<SermonRecord> two = await db.Sermons.OrderBy(sermon => sermon.PreachedAt).Take(2).ToListAsync(Cancel);
            await db.Sermons.Where(sermon => !two.Contains(sermon)).ExecuteDeleteAsync(Cancel);
            foreach (SermonRecord sermon in two)
            {
                string key = $"sermons/audio/{Guid.CreateVersion7():N}.mp3";
                await Blobs.PutAsync(BlobBucket.Public, key, new MemoryStream(recording), new BlobHeaders("audio/mpeg", "public"), Cancel);
                sermon.AudioUrl = Blobs.PublicUrlOf(key).ToString();
            }
            await db.SaveChangesAsync(Cancel);
            return two;
        });

        await WithAsync(provider => provider.GetRequiredService<AudioKeeper>().RunAsync(Cancel));

        List<SermonRecord> kept = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking().ToListAsync(Cancel));
        Assert.Single(kept.Select(sermon => sermon.AudioUrl).Distinct());
        Assert.Single(Blobs.Objects.Keys, stored => stored.Item2.EndsWith(".mp3", StringComparison.Ordinal));
        Assert.Single(Media.Stored);
        Assert.All(sermons, sermon => Assert.Equal([kept.Single(one => one.Id == sermon.Id).AudioMedia!.Id], Media.Uses[SermonMedia.AudioOwner(sermon.Id)]));
    }
}
