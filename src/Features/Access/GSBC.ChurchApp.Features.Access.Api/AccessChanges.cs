using GSBC.ChurchApp.Features.Access.Api.Data;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessChanges(AccessDbContext db, GrantsFeed grants)
{
    public async Task AnnounceAsync(CancellationToken cancellationToken)
    {
        await db.AnnounceChangeAsync(cancellationToken);
        await grants.PublishAsync(cancellationToken);
    }
}
