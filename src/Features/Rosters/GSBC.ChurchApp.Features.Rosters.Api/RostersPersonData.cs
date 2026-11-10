using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed class RostersPersonData(RostersDbContext db, RosterPublisher publisher, TimeProvider time) : IPersonData
{
    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<Guid> coming = await db.Assignments.Where(assignment => assignment.PersonId == userId)
            .Join(db.Dates.Where(date => date.StartsAt > now), assignment => assignment.DateId, date => date.Id, (assignment, _) => assignment.Id)
            .ToListAsync(cancellationToken);
        List<Guid> ministries = await db.Assignments.Where(assignment => assignment.PersonId == userId).Select(assignment => assignment.MinistryId).Distinct()
            .ToListAsync(cancellationToken);

        await db.Swaps.Where(swap => coming.Contains(swap.AssignmentId) || swap.AskedBy == userId || swap.AskedTo == userId || swap.OfferedBy == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await db.Assignments.Where(assignment => coming.Contains(assignment.Id)).ExecuteDeleteAsync(cancellationToken);
        await db.Assignments.Where(assignment => assignment.PersonId == userId)
            .ExecuteUpdateAsync(set => set.SetProperty(assignment => assignment.PersonId, (Guid?)null).SetProperty(assignment => assignment.Reason, ""), cancellationToken);
        await db.Assignments.Where(assignment => assignment.RepliedBy == userId)
            .ExecuteUpdateAsync(set => set.SetProperty(assignment => assignment.RepliedBy, (Guid?)null), cancellationToken);
        await db.Away.Where(away => away.PersonId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Trained.Where(trained => trained.PersonId == userId).ExecuteDeleteAsync(cancellationToken);

        await publisher.ForgetAsync(userId, cancellationToken);
        foreach (Guid ministry in ministries)
            await publisher.PublishAsync(ministry, cancellationToken);
    }
}
