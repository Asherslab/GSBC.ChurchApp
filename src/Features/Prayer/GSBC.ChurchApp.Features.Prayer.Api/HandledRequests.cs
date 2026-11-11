using GSBC.ChurchApp.Features.Prayer.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class HandledRequests(PrayerDbContext db, TimeProvider time)
{
    public static readonly TimeSpan KeptFor = TimeSpan.FromDays(7);

    public async Task<bool> AlreadyAsync(string? requestId, CancellationToken cancellationToken)
    {
        if (requestId is null)
            return false;
        if (await db.HandledRequests.AsNoTracking().AnyAsync(handled => handled.Id == requestId, cancellationToken))
            return true;
        db.HandledRequests.Add(new HandledRequestRecord { Id = requestId, HandledAt = time.GetUtcNow().UtcDateTime });
        return false;
    }

    public async Task ForgetOldAsync(CancellationToken cancellationToken)
    {
        DateTime before = time.GetUtcNow().UtcDateTime - KeptFor;
        await db.HandledRequests.Where(handled => handled.HandledAt < before).ExecuteDeleteAsync(cancellationToken);
    }
}
