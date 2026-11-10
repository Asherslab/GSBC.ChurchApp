using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryMover(MinistriesDbContext db, MinistryChanges changes, IRoleAssignments roles, IPeopleIndex people, TimeProvider time)
{
    public async Task<MovePreview> PreviewAsync(MinistryTree tree, MinistryRecord ministry, MinistryRecord? under, CancellationToken cancellationToken)
    {
        MoveEffects effects = MoveEffects.Of(tree, ministry, under);
        List<Guid> still = await LeadersAsync([ministry.Id, .. effects.Staying], cancellationToken);
        List<Guid> leaving = await LeadersAsync(effects.Leaving.Select(place => place.Id), cancellationToken);
        List<Guid> joining = await LeadersAsync(effects.Joining.Select(place => place.Id), cancellationToken);
        return new MovePreview
        {
            Name = ministry.Name,
            From = PathOf(tree, ministry.ParentId),
            To = PathOf(tree, under?.Id),
            SubMinistries = tree.Below(ministry.Id).Count,
            Lose = await ChangesAsync(effects.Leaving, [.. still, .. joining], cancellationToken),
            Gain = await ChangesAsync(effects.Joining, [.. still, .. leaving], cancellationToken),
            Leaves = [.. effects.LeavesGathering],
            Joins = [.. effects.JoinsGathering],
        };
    }

    public async Task<MinistryRecord> MoveAsync(MinistryTree tree, MinistryRecord ministry, MinistryRecord? under, CancellationToken cancellationToken)
    {
        MinistryRecord moving = await db.Ministries.SingleAsync(found => found.Id == ministry.Id, cancellationToken);
        Guid? from = moving.ParentId;
        moving.ParentId = under?.Id;
        moving.Order = (tree.All.Where(other => other.ParentId == under?.Id).Select(other => (int?)other.Order).Max() ?? -1) + 1;
        moving.UpdatedAt = time.GetUtcNow().UtcDateTime;
        foreach (Guid touched in new[] { moving.Id }.Concat(tree.Below(moving.Id).Select(below => below.Id)))
            changes.Touch(touched);
        foreach (Guid? end in new[] { from, under?.Id })
            if (end is { } parent)
                changes.Touch(parent);
        await db.SaveChangesAsync(cancellationToken);
        await roles.ScopesChangedAsync(cancellationToken);
        return moving;
    }

    private async Task<List<Guid>> LeadersAsync(IEnumerable<Guid> places, CancellationToken cancellationToken)
    {
        List<Guid> leaders = [];
        foreach (Guid place in places)
            foreach (string role in new[] { Roles.Leader, Roles.Admin })
                leaders.AddRange((await roles.HoldersAsync(Roles.KeyOf(role), place.ToString(), cancellationToken)).Select(holder => holder.UserId));
        return leaders;
    }

    private async Task<List<LeadChange>> ChangesAsync(IReadOnlyList<MinistryRecord> places, IEnumerable<Guid> unchanged, CancellationToken cancellationToken)
    {
        List<LeadChange> changed = [];
        HashSet<Guid> told = [.. unchanged];
        foreach (MinistryRecord place in places)
        {
            List<Guid> leaders = (await LeadersAsync([place.Id], cancellationToken)).Where(told.Add).ToList();
            if (leaders.Count == 0)
                continue;
            IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(leaders, cancellationToken);
            changed.Add(new LeadChange
            {
                Ministry = place.Name,
                People = leaders.Select(leader => names.GetValueOrDefault(leader) ?? "Someone").Order(StringComparer.CurrentCulture).ToList(),
            });
        }
        return changed;
    }

    private static string PathOf(MinistryTree tree, Guid? id) =>
        id is { } place && tree.Find(place) is { } found
            ? string.Join(" › ", tree.Above(place).Reverse().Append(found).Select(ministry => ministry.Name))
            : "";
}

internal sealed record MoveEffects(
    IReadOnlyList<MinistryRecord> Leaving, IReadOnlyList<MinistryRecord> Joining, IReadOnlyList<Guid> Staying,
    IReadOnlyList<string> LeavesGathering, IReadOnlyList<string> JoinsGathering)
{
    public static MoveEffects Of(MinistryTree tree, MinistryRecord ministry, MinistryRecord? under)
    {
        MinistryTree after = new(tree.All.Select(each => each.Id == ministry.Id ? each.Under(under?.Id) : each));
        IReadOnlyList<MinistryRecord> before = tree.Above(ministry.Id);
        IReadOnlyList<MinistryRecord> then = after.Above(ministry.Id);
        List<string> leaves = [], joins = [];
        foreach (MinistryRecord gathering in tree.All.Where(each => each.Id != ministry.Id).OrderBy(each => each.Name, StringComparer.CurrentCulture))
            foreach (string part in gathering.PartsFromBelow)
            {
                bool was = MinistryViews.Gathered(tree, gathering.Id, part).Contains(ministry.Id);
                bool will = MinistryViews.Gathered(after, gathering.Id, part).Contains(ministry.Id);
                string words = $"{MinistryNames.Possessive(gathering.Name)} {MinistryParts.Of(part).Name.ToLowerInvariant()}";
                if (was && !will)
                    leaves.Add(words);
                else if (will && !was)
                    joins.Add(words);
            }
        return new MoveEffects(
            before.Where(old => then.All(next => next.Id != old.Id)).ToList(),
            then.Where(next => before.All(old => old.Id != next.Id)).ToList(),
            before.Where(old => then.Any(next => next.Id == old.Id)).Select(old => old.Id).ToList(),
            leaves, joins);
    }
}
