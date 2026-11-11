using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class PrayerPersonData(PrayerDbContext db, PrayerPublisher publisher, TimeProvider time) : IPersonData
{
    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        foreach (RequestRecord request in await db.Requests.Where(request => request.UserId == userId).ToListAsync(cancellationToken))
        {
            db.Requests.Remove(request);
            publisher.Deleted(request, now);
        }

        List<PrayedRecord> prayers = await db.Prayed.Where(prayed => prayed.UserId == userId).ToListAsync(cancellationToken);
        HashSet<Guid> prayedFor = prayers.Select(prayed => prayed.RequestId).ToHashSet();
        db.Prayed.RemoveRange(prayers);
        foreach (RequestRecord request in await db.Requests.Where(request => prayedFor.Contains(request.Id) && request.UserId != userId).ToListAsync(cancellationToken))
        {
            request.Prayed = Math.Max(0, request.Prayed - 1);
            await publisher.PublishAsync(request, now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
