using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class PrayerKeeping(PrayerDbContext db, PrayerPublisher publisher, PrayerTelling telling, TimeProvider time)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<RequestRecord> leaving = await db.Requests.Where(request => request.Wall == WallState.Shown && request.WallUntil <= now).ToListAsync(cancellationToken);
        foreach (RequestRecord request in leaving.Where(request => PrayerRules.HasLeftTheWall(request, now)))
        {
            request.Wall = WallState.Ended;
            request.ChangedAt = now;
            request.Steps.Add(new StepRecord { At = now, What = "Left the prayer wall" });
            publisher.Changed(request, now);
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
        foreach (RequestRecord request in old)
            await telling.SettledAsync(request, cancellationToken);
    }
}
