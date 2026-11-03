using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Data;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessChanges(AccessDbContext db, GrantsFeed grants, TimeProvider time)
{
    private const string Everything = "roles";

    public async Task AnnounceAsync(CancellationToken cancellationToken)
    {
        await db.AnnounceChangeAsync(cancellationToken);
        db.FeedChanges.Add(FeedChangeRecord.Marker(AccessFeeds.Roles, Everything, time.GetUtcNow().UtcDateTime));
        await grants.PublishAsync(cancellationToken);
    }
}
