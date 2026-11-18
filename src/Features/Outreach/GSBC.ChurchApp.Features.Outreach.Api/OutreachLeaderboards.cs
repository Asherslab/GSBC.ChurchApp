using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachLeaderboards(OutreachDbContext db, OutreachPublisher publisher, IPeopleIndex people)
{
    public async Task<IReadOnlyDictionary<Guid, Walker>> WalkersAsync(IEnumerable<WalkRecord> walks, CancellationToken cancellationToken)
    {
        HashSet<Guid> users = walks.Where(walk => walk.UserId is not null).Select(walk => walk.UserId!.Value).ToHashSet();
        if (users.Count == 0)
            return new Dictionary<Guid, Walker>();
        Dictionary<Guid, bool> byName = await db.Walkers.AsNoTracking().Where(walker => users.Contains(walker.UserId))
            .ToDictionaryAsync(walker => walker.UserId, walker => walker.ByName, cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(users, cancellationToken);
        return users.ToDictionary(user => user, user => new Walker(user, names.GetValueOrDefault(user, ""), byName.GetValueOrDefault(user, true)));
    }

    public async Task PublishAsync(Guid campaign, DateTime now, CancellationToken cancellationToken)
    {
        List<WalkRecord> walks = await db.Walks.AsNoTracking().Where(walk => walk.Campaigns.Contains(campaign) && walk.Doubtful == "").ToListAsync(cancellationToken);
        Leaderboard board = TotalsRules.Leaderboard(campaign, walks, await WalkersAsync(walks, cancellationToken));
        string id = campaign.ToString();
        byte[]? latest = await db.FeedChanges.AsNoTracking().Where(change => change.Feed == OutreachFeeds.Leaderboards && change.RecordId == id)
            .OrderByDescending(change => change.Cursor).Select(change => change.Payload).FirstOrDefaultAsync(cancellationToken);
        if (latest is null || !latest.AsSpan().SequenceEqual(OutreachPayloads.Encode(board)))
            publisher.Leaderboard(board, now);
    }

    public async Task PublishAllAsync(DateTime now, CancellationToken cancellationToken)
    {
        foreach (Guid campaign in await db.Campaigns.AsNoTracking().Where(campaign => campaign.Leaderboard).Select(campaign => campaign.Id).ToListAsync(cancellationToken))
            await PublishAsync(campaign, now, cancellationToken);
    }
}
