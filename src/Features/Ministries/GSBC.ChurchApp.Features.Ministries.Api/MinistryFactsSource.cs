using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Ministries;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryFactsSource(MinistryViews views) : IMinistryFacts
{
    public async ValueTask<MinistryFacts?> FindAsync(Guid ministryId, CancellationToken cancellationToken) =>
        (await views.EveryTreeAsync(cancellationToken)).Find(ministryId) is { } ministry ? Facts(ministry) : null;

    public async ValueTask<IReadOnlyList<MinistryFacts>> UsingAsync(string part, CancellationToken cancellationToken) =>
        (await views.EveryTreeAsync(cancellationToken)).All.Where(ministry => MinistryViews.IsOn(ministry, part)).Select(Facts).ToList();

    public async ValueTask<IReadOnlyList<MinistryMember>> TeamAsync(Guid ministryId, CancellationToken cancellationToken)
    {
        MinistryTree tree = await views.EveryTreeAsync(cancellationToken);
        if (tree.Find(ministryId) is null)
            return [];

        Dictionary<Guid, bool> team = [];
        foreach (Guid person in await views.HoldersAsync(Roles.Team, ministryId, cancellationToken))
            team[person] = false;
        foreach (Guid ministry in tree.Above(ministryId).Select(above => above.Id).Prepend(ministryId))
            foreach (string role in new[] { Roles.Leader, Roles.Admin })
                foreach (Guid person in await views.HoldersAsync(role, ministry, cancellationToken))
                    team[person] = true;
        return team.Select(member => new MinistryMember(member.Key, member.Value)).ToList();
    }

    private static MinistryFacts Facts(MinistryRecord ministry) => new(ministry.Id, ministry.Name, Archived: ministry.ArchivedAt is not null, ministry.PartsOff);
}
