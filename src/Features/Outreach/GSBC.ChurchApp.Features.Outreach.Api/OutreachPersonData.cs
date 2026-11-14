using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachPersonData(OutreachDbContext db, OutreachPublisher publisher, TimeProvider time) : IPersonData
{
    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        foreach (WalkRecord walk in await db.Walks.Where(walk => walk.UserId == userId).ToListAsync(cancellationToken))
        {
            walk.UserId = null;
            publisher.WalkGone(walk.Id, userId, now);
        }
        if (await db.Walkers.FirstOrDefaultAsync(found => found.UserId == userId, cancellationToken) is { } walker)
        {
            db.Walkers.Remove(walker);
            publisher.CountingGone(userId, now);
        }
        foreach (ClaimRecord claim in await db.Claims.Where(claim => claim.UserId == userId).ToListAsync(cancellationToken))
        {
            db.Claims.Remove(claim);
            publisher.ClaimGone(claim.Id, now);
        }
        await db.Skips.Where(skip => skip.UserId == userId).ExecuteUpdateAsync(set => set.SetProperty(skip => skip.UserId, (Guid?)null), cancellationToken);
        await db.Reports.Where(report => report.UserId == userId).ExecuteUpdateAsync(set => set.SetProperty(report => report.UserId, (Guid?)null), cancellationToken);
        await db.FollowUps.Where(followUp => followUp.UserId == userId)
            .ExecuteUpdateAsync(set => set.SetProperty(followUp => followUp.UserId, (Guid?)null), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
