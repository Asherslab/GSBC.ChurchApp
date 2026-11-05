using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class MediaSizesBackfill(IServiceScopeFactory scopes, TimeProvider time, ILogger<MediaSizesBackfill> logger) : BackgroundService
{
    private const int Batch = 10;
    private const string SermonArtworkArea = "sermons";
    private const string LargeCopy = "____________-large.jpg";
    private const string SmallCopy = "____________-small.jpg";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ReportUnpairedSermonArtworkAsync(stoppingToken);
            while (await SizeWaitingAsync(Batch, stoppingToken) > 0)
            {
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Stopped making picture sizes for the library; the rest are made the next time the API starts");
        }
    }

    private async Task ReportUnpairedSermonArtworkAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        MediaDbContext db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        int unpaired = await db.Items
            .Where(item => db.Folders.Any(folder => folder.Id == item.FolderId && folder.SystemArea == SermonArtworkArea))
            .CountAsync(item => EF.Functions.Like(item.FileName, SmallCopy)
                || (EF.Functions.Like(item.FileName, LargeCopy) && !db.Renditions.Any(rendition => rendition.MediaId == item.Id)), cancellationToken);
        if (unpaired > 0)
            logger.LogInformation("{Unpaired} sermon artwork copies weren't paired into one picture with its sizes, so they stay separate in the library", unpaired);
    }

    private async Task<int> SizeWaitingAsync(int batch, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        MediaDbContext db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        MediaSizer sizer = scope.ServiceProvider.GetRequiredService<MediaSizer>();

        List<MediaItem> waiting = await db.Items.Where(item => item.Sizing == PictureSizing.Waiting).OrderBy(item => item.UploadedAt).Take(batch).ToListAsync(cancellationToken);
        foreach (MediaItem item in waiting)
        {
            try
            {
                await sizer.SizeStoredAsync(item, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Couldn't make sizes of {Key}, so it's kept as it is", item.Key);
                item.Sizing = PictureSizing.KeptAsIs;
            }
            db.FeedChanges.Add(FeedChangeRecord.Marker(MediaFeeds.Library, item.Id.ToString(), time.GetUtcNow().UtcDateTime));
        }
        await db.SaveChangesAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<MediaHolders>().RefreshAsync([.. waiting.Select(item => item.Id)], cancellationToken);
        return waiting.Count;
    }
}
