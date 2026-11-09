using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessPersonData(AccessDbContext db, AccessChanges changes, TimeProvider time) : IPersonData
{
    public async ValueTask<string?> WhyKeepAsync(Guid userId, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<Assignment> owners = await db.ActiveAssignments(now).Include(assignment => assignment.Role)
            .Where(assignment => assignment.Role!.SystemKey == AccessService.OwnerKey).ToListAsync(cancellationToken);
        return AssignmentPolicy.OnlyOwner(owners, userId) ? "This account is the church's last Owner. Make someone else an Owner first." : null;
    }

    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (await db.Assignments.Where(assignment => assignment.UserId == userId).ExecuteDeleteAsync(cancellationToken) > 0)
            await changes.AnnounceAsync(cancellationToken);
    }
}
