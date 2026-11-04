using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class AudioCopier(IServiceScopeFactory scopes, IHostEnvironment environment, ILogger<AudioCopier> logger) : BackgroundService
{
    public const int DevelopmentLimit = 20;
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(1);
    private const long AdvisoryLockKey = 0x6773_6263_636f_7079;

    public int? Limit => environment.IsDevelopment() ? DevelopmentLimit : null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Every);
        do
        {
            try
            {
                await CopyAsync(stoppingToken);
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

    private async Task CopyAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        SermonsDbContext db = scope.ServiceProvider.GetRequiredService<SermonsDbContext>();
        if ((await SourceStates.GetAsync(db, cancellationToken)).Source != SermonSources.Uploads)
            return;
        await db.Database.OpenConnectionAsync(cancellationToken);
        if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
            return;
        try
        {
            int copiedAlready = await db.Sermons.CountAsync(sermon => sermon.AudioCopiedFrom != null, cancellationToken);
            int room = Limit is { } limit ? Math.Max(0, limit - copiedAlready) : int.MaxValue;
            IBlobStore blobs = scope.ServiceProvider.GetRequiredService<IBlobStore>();
            WebsiteClient website = scope.ServiceProvider.GetRequiredService<WebsiteClient>();
            SermonPublisher publisher = scope.ServiceProvider.GetRequiredService<SermonPublisher>();
            int copied = 0;
            foreach (SermonRecord sermon in await Waiting(db).Take(Math.Min(room, 500)).ToListAsync(cancellationToken))
            {
                await CopyOneAsync(sermon, blobs, website, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                if (++copied % 10 == 0)
                    await publisher.PublishAsync(cancellationToken);
            }
            if (copied > 0)
            {
                await publisher.PublishAsync(cancellationToken);
                await scope.ServiceProvider.GetRequiredService<PodcastFeedWriter>().WriteAsync(cancellationToken);
            }
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
        }
    }

    private static async Task CopyOneAsync(SermonRecord sermon, IBlobStore blobs, WebsiteClient website, CancellationToken cancellationToken)
    {
        string extension = sermon.AudioType == "audio/mp4" ? "m4a" : "mp3";
        string key = $"sermons/audio/{sermon.Id:N}.{extension}";
        string file = Path.Combine(Path.GetTempPath(), $"gsbc-sermon-{sermon.Id:N}");
        try
        {
            await using (Stream download = await website.StreamAsync(sermon.AudioUrl, cancellationToken))
            await using (FileStream local = File.Create(file))
                await download.CopyToAsync(local, cancellationToken);
            await using (FileStream upload = File.OpenRead(file))
                await blobs.PutAsync(BlobBucket.Public, key, upload, new BlobHeaders(sermon.AudioType, ContentAddress.ImmutableCacheControl), cancellationToken);
            sermon.AudioCopiedFrom = sermon.AudioUrl;
            sermon.AudioUrl = blobs.PublicUrlOf(key).ToString();
            sermon.AudioBytes = new FileInfo(file).Length;
        }
        finally
        {
            File.Delete(file);
        }
    }
}
