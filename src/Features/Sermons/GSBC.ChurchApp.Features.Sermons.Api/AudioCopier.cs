using System.Security.Cryptography;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class AudioCopier(IServiceScopeFactory scopes, IHostEnvironment environment, TimeProvider time, ILogger<AudioCopier> logger) : BackgroundService
{
    public const int DevelopmentLimit = 20;
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ProgressEvery = TimeSpan.FromSeconds(1);
    private const int ChunkBytes = 256 * 1024;
    private const long AdvisoryLockKey = 0x6773_6263_636f_7079;

    public static readonly MediaSystemArea Area = new("sermons-audio", "Sermon audio",
        "The sermons' audio in the church's own storage: what the AV team uploads and what was copied from the website. Each recording is kept once.",
        SermonsPermissions.Edit);

    public int? Limit => environment.IsDevelopment() ? DevelopmentLimit : null;

    public static string AudioOwner(Guid id) => $"sermons/sermon/{id}/audio";

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
                logger.LogError(exception, "Copying sermon audio into our storage failed; trying again shortly");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public static IQueryable<SermonRecord> Waiting(SermonsDbContext db) =>
        db.Sermons.Where(sermon => sermon.Origin == SermonSources.Website && sermon.AudioCopiedFrom == null && sermon.AudioUrl != "")
            .OrderByDescending(sermon => sermon.PreachedAt);

    public static IQueryable<SermonRecord> Unkept(SermonsDbContext db) =>
        db.Sermons.Where(sermon => sermon.AudioMedia == null && (sermon.AudioCopiedFrom != null || sermon.Origin == SermonSources.Uploads))
            .OrderByDescending(sermon => sermon.PreachedAt);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        SermonsDbContext db = scope.ServiceProvider.GetRequiredService<SermonsDbContext>();
        await db.Database.OpenConnectionAsync(cancellationToken);
        if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
            return;
        try
        {
            Storage storage = new(db, scope.ServiceProvider.GetRequiredService<IBlobStore>(), scope.ServiceProvider.GetRequiredService<IMediaSystemFolders>(),
                scope.ServiceProvider.GetRequiredService<IMediaReferences>(), time);
            bool ours = (await SourceStates.GetAsync(db, cancellationToken)).Source == SermonSources.Uploads;
            int copied = ours ? await CopyAsync(scope.ServiceProvider, storage, cancellationToken) : 0;
            int moved = 0;
            foreach (SermonRecord sermon in await Unkept(db).Take(50).ToListAsync(cancellationToken))
                if (await storage.KeepStoredAsync(sermon, cancellationToken))
                    moved++;
            if (copied + moved == 0)
                return;
            await scope.ServiceProvider.GetRequiredService<SermonPublisher>().PublishAsync(cancellationToken);
            if (ours)
                await scope.ServiceProvider.GetRequiredService<PodcastFeedWriter>().WriteAsync(cancellationToken);
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
        }
    }

    private async Task<int> CopyAsync(IServiceProvider services, Storage storage, CancellationToken cancellationToken)
    {
        SermonsDbContext db = storage.Db;
        int copiedAlready = await db.Sermons.CountAsync(sermon => sermon.AudioCopiedFrom != null, cancellationToken);
        int room = Limit is { } limit ? Math.Max(0, limit - copiedAlready) : int.MaxValue;
        WebsiteClient website = services.GetRequiredService<WebsiteClient>();
        int copied = 0;
        foreach (SermonRecord sermon in await Waiting(db).Take(Math.Min(room, 500)).ToListAsync(cancellationToken))
        {
            if (await storage.CopyAsync(sermon, website, cancellationToken))
                copied++;
        }
        return copied;
    }

    private sealed class Storage(SermonsDbContext db, IBlobStore blobs, IMediaSystemFolders folders, IMediaReferences references, TimeProvider time)
    {
        public SermonsDbContext Db => db;

        public async Task<bool> CopyAsync(SermonRecord sermon, WebsiteClient website, CancellationToken cancellationToken)
        {
            string file = Path.Combine(Path.GetTempPath(), $"gsbc-sermon-{sermon.Id:N}");
            sermon.CopyStartedAt = Now;
            sermon.CopiedBytes = 0;
            sermon.CopyProblem = null;
            await SaveAsync(sermon, cancellationToken);
            try
            {
                string sha256 = await DownloadAsync(sermon, website, file, cancellationToken);
                long bytes = new FileInfo(file).Length;
                string key = ContentAddress.KeyFor(sha256, sermon.AudioType == "audio/mp4" ? "m4a" : "mp3");
                if (await blobs.FindAsync(BlobBucket.Public, key, cancellationToken) is null)
                {
                    await using FileStream upload = File.OpenRead(file);
                    await blobs.PutAsync(BlobBucket.Public, key, upload, new BlobHeaders(sermon.AudioType, ContentAddress.ImmutableCacheControl), cancellationToken);
                }
                sermon.AudioCopiedFrom = sermon.AudioUrl;
                sermon.AudioUrl = blobs.PublicUrlOf(key).ToString();
                sermon.AudioBytes = bytes;
                sermon.CopiedBytes = bytes;
                sermon.CopyStartedAt = null;
                MediaRef kept = await KeepAsync(sermon, new StoredFile(key, sha256, bytes, sermon.AudioType, FileNameOf(sermon)), cancellationToken);
                if (kept.Key != key && !await db.Sermons.AnyAsync(other => other.AudioUrl == blobs.PublicUrlOf(key).ToString(), cancellationToken))
                    await blobs.DeleteAsync(BlobBucket.Public, key, cancellationToken);
                return true;
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException || exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                sermon.CopyStartedAt = null;
                sermon.CopyProblem = exception switch
                {
                    HttpRequestException { StatusCode: { } status } => $"The website answered {(int)status}.",
                    TaskCanceledException => "The website took too long to answer.",
                    _ => "The copy was cut short.",
                };
                await SaveAsync(sermon, cancellationToken);
                return false;
            }
            finally
            {
                File.Delete(file);
            }
        }

        public async Task<bool> KeepStoredAsync(SermonRecord sermon, CancellationToken cancellationToken)
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
            MediaRef kept = await KeepAsync(sermon, new StoredFile(key, sha256, stored.Bytes, sermon.AudioType, FileNameOf(sermon)), cancellationToken);
            if (kept.Key == key)
                return false;
            if (!await db.Sermons.AnyAsync(other => other.AudioUrl == storedAt, cancellationToken))
                await blobs.DeleteAsync(BlobBucket.Public, key, cancellationToken);
            return true;
        }

        private async Task<MediaRef> KeepAsync(SermonRecord sermon, StoredFile file, CancellationToken cancellationToken)
        {
            MediaRef kept = await folders.KeepStoredAsync(Area, file, cancellationToken);
            await references.UseAsync(AudioOwner(sermon.Id), [kept], cancellationToken);
            sermon.AudioMedia = kept;
            sermon.AudioUrl = blobs.PublicUrlOf(kept.Key).ToString();
            await SaveAsync(sermon, cancellationToken);
            return kept;
        }

        private async Task<string> DownloadAsync(SermonRecord sermon, WebsiteClient website, string file, CancellationToken cancellationToken)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[] buffer = new byte[ChunkBytes];
            DateTime told = Now;
            await using Stream download = await website.StreamAsync(sermon.AudioUrl, cancellationToken);
            await using FileStream local = File.Create(file);
            int read;
            while ((read = await download.ReadAsync(buffer, cancellationToken)) > 0)
            {
                hash.AppendData(buffer, 0, read);
                await local.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                sermon.CopiedBytes += read;
                if (Now - told >= ProgressEvery)
                {
                    told = Now;
                    await SaveAsync(sermon, cancellationToken);
                }
            }
            return Convert.ToHexStringLower(hash.GetHashAndReset());
        }

        private async Task SaveAsync(SermonRecord sermon, CancellationToken cancellationToken)
        {
            db.FeedChanges.Add(FeedChangeRecord.Marker(SermonsFeeds.Copying, sermon.Id.ToString(), Now));
            await db.SaveChangesAsync(cancellationToken);
        }

        private static string FileNameOf(SermonRecord sermon) =>
            $"{sermon.PreachedAt:yyyy-MM-dd} {sermon.EffectiveTitle}.{(sermon.AudioType == "audio/mp4" ? "m4a" : "mp3")}";

        private DateTime Now => time.GetUtcNow().UtcDateTime;
    }
}
