using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SermonsPersonData(SermonsDbContext db, TimeProvider time) : IPersonData
{
    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        long version = time.GetUtcNow().UtcDateTime.Ticks;
        foreach (ProgressRecord progress in await db.Progress.Where(progress => progress.UserId == userId).ToListAsync(cancellationToken))
        {
            db.Progress.Remove(progress);
            db.FeedChanges.Add(new FeedChangeRecord
            {
                Feed = SermonsFeeds.Progress, RecordId = $"{userId:N}-{progress.SermonId:N}", ScopeId = userId.ToString(), Version = version, Deleted = true,
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
