using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Data;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryChanges(MinistriesDbContext db, TimeProvider time)
{
    public void Touch(Guid ministryId, bool deleted = false)
    {
        string id = ministryId.ToString();
        db.FeedChanges.Add(new FeedChangeRecord
        {
            Feed = MinistriesFeeds.Ministries, RecordId = id, Version = time.GetUtcNow().UtcDateTime.Ticks, Deleted = deleted,
        });
    }
}
