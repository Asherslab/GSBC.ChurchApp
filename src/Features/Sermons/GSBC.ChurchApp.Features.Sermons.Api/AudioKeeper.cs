using System.Security.Cryptography;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class AudioKeeper(IServiceScopeFactory scopes, ILogger<AudioKeeper> logger) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(1);
    private const long AdvisoryLockKey = 0x6773_6263_636f_7079;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Every);
        do
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Keeping uploaded sermon audio in Media failed; trying again shortly");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private static IQueryable<SermonRecord> Unkept(SermonsDbContext db) =>
        db.Sermons.Where(sermon => sermon.AudioMedia == null && sermon.AudioUrl != "").OrderByDescending(sermon => sermon.PreachedAt);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        SermonsDbContext db = scope.ServiceProvider.GetRequiredService<SermonsDbContext>();
        await db.Database.OpenConnectionAsync(cancellationToken);
        if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
            return;
        try
        {
            IBlobStore blobs = scope.ServiceProvider.GetRequiredService<IBlobStore>();
            IMediaSystemFolders folders = scope.ServiceProvider.GetRequiredService<IMediaSystemFolders>();
            IMediaReferences references = scope.ServiceProvider.GetRequiredService<IMediaReferences>();
            int kept = 0;
            foreach (SermonRecord sermon in await Unkept(db).Take(50).ToListAsync(cancellationToken))
                if (await KeepAsync(db, blobs, folders, references, sermon, cancellationToken))
                    kept++;
            if (kept > 0)
                await scope.ServiceProvider.GetRequiredService<SermonPublisher>().PublishAsync(cancellationToken);
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
        }
    }

    private static async Task<bool> KeepAsync(SermonsDbContext db, IBlobStore blobs, IMediaSystemFolders folders, IMediaReferences references, SermonRecord sermon,
        CancellationToken cancellationToken)
    {
        string ours = blobs.PublicUrlOf("").ToString();
        string storedAt = sermon.AudioUrl;
        if (!storedAt.StartsWith(ours, StringComparison.Ordinal))
            return false;
        string key = storedAt[ours.Length..];
        if (await blobs.FindAsync(BlobBucket.Public, key, cancellationToken) is not { } stored)
            return false;
        string sha256;
        await using (Stream audio = await blobs.OpenAsync(BlobBucket.Public, key, cancellationToken))
            sha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(audio, cancellationToken));
        MediaRef kept = await folders.KeepStoredAsync(SermonMedia.Audio, new StoredFile(key, sha256, stored.Bytes, sermon.AudioType, FileNameOf(sermon)), cancellationToken);
        await references.UseAsync(SermonMedia.AudioOwner(sermon.Id), [kept], cancellationToken);
        sermon.AudioMedia = kept;
        sermon.AudioUrl = blobs.PublicUrlOf(kept.Key).ToString();
        await db.SaveChangesAsync(cancellationToken);
        if (kept.Key != key && !await db.Sermons.AnyAsync(other => other.AudioUrl == storedAt, cancellationToken))
            await blobs.DeleteAsync(BlobBucket.Public, key, cancellationToken);
        return true;
    }

    private static string FileNameOf(SermonRecord sermon) =>
        $"{sermon.PreachedAt:yyyy-MM-dd} {sermon.Title}.{(sermon.AudioType == "audio/mp4" ? "m4a" : "mp3")}";
}
