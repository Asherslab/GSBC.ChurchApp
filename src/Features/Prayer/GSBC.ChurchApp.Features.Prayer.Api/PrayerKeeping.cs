using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class PrayerKeeping(PrayerDbContext db, PrayerPublisher publisher, PrayerTelling telling, HandledRequests handled, TimeProvider time)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        await FillStaffFeedsAsync(now, cancellationToken);
        List<RequestRecord> leaving = await db.Requests.Where(request => request.Wall == WallState.Shown && request.WallUntil <= now).ToListAsync(cancellationToken);
        foreach (RequestRecord request in leaving.Where(request => PrayerRules.HasLeftTheWall(request, now)))
        {
            request.Wall = WallState.Ended;
            request.ChangedAt = now;
            request.Steps.Add(new StepRecord { At = now, What = "Left the prayer wall" });
            await publisher.ChangedAsync(request, now, cancellationToken);
        }

        DateTime keptSince = now - PrayerRules.KeptFor;
        List<RequestRecord> old = (await db.Requests.Where(request => request.ChangedAt <= keptSince).ToListAsync(cancellationToken))
            .Where(request => PrayerRules.IsDueForDeletion(request, now)).ToList();
        foreach (RequestRecord request in old)
        {
            db.Requests.Remove(request);
            publisher.Deleted(request, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        await handled.ForgetOldAsync(cancellationToken);
        foreach (RequestRecord request in old)
            await telling.SettledAsync(request, cancellationToken);
    }

    private async Task FillStaffFeedsAsync(DateTime now, CancellationToken cancellationToken)
    {
        if (await db.FeedChanges.AnyAsync(change => change.Feed == PrayerFeeds.Approving || change.Feed == PrayerFeeds.Care, cancellationToken))
            return;
        foreach (RequestRecord request in await db.Requests.Where(request => request.Care == CareState.New || request.Wall == WallState.Waiting || request.Wall == WallState.Shown)
                     .ToListAsync(cancellationToken))
            await publisher.PublishAsync(request, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
