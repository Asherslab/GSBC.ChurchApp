using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryTree(IEnumerable<MinistryRecord> ministries)
{
    private readonly Dictionary<Guid, MinistryRecord> _byId = ministries.ToDictionary(ministry => ministry.Id);

    public IReadOnlyCollection<MinistryRecord> All => _byId.Values;

    public MinistryRecord? Find(Guid id) => _byId.GetValueOrDefault(id);

    public IEnumerable<MinistryRecord> ChildrenOf(Guid id) =>
        _byId.Values.Where(ministry => ministry.ParentId == id).OrderBy(ministry => ministry.Order).ThenBy(ministry => ministry.Name, StringComparer.CurrentCulture);

    public IReadOnlyList<MinistryRecord> Above(Guid id)
    {
        List<MinistryRecord> above = [];
        for (MinistryRecord? next = Find(id) is { ParentId: { } parent } ? Find(parent) : null;
             next is not null && above.All(seen => seen.Id != next.Id) && next.Id != id;
             next = next.ParentId is { } grand ? Find(grand) : null)
            above.Add(next);
        return above;
    }

    public IReadOnlyList<MinistryRecord> Below(Guid id)
    {
        List<MinistryRecord> below = [];
        Queue<Guid> waiting = new([id]);
        while (waiting.TryDequeue(out Guid next))
            foreach (MinistryRecord child in ChildrenOf(next))
                if (child.Id != id && below.All(seen => seen.Id != child.Id))
                {
                    below.Add(child);
                    waiting.Enqueue(child.Id);
                }
        return below;
    }

    public IReadOnlyList<MinistryRecord> LifeGroups() =>
        _byId.Values.Where(ministry => ministry.ShowsInLifeGroups)
            .OrderBy(group => group.Order).ThenBy(group => group.Name, StringComparer.CurrentCulture).ToList();

    public bool WouldLoop(Guid id, Guid? newParent) =>
        newParent is { } parent && (parent == id || Below(id).Any(child => child.Id == parent));
}
