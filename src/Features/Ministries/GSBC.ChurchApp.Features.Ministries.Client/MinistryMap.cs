using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed class MinistryMap
{
    private readonly Dictionary<string, MinistryCard> _byId;

    public MinistryMap(IEnumerable<MinistryCard> cards)
    {
        All = cards.OrderBy(card => card.Ministry.Order).ThenBy(card => card.Ministry.Name, StringComparer.CurrentCulture).ToList();
        _byId = All.ToDictionary(card => card.Ministry.Id);
    }

    public IReadOnlyList<MinistryCard> All { get; }

    public MinistryCard? Find(string id) => _byId.GetValueOrDefault(id);

    public IEnumerable<MinistryCard> ChildrenOf(string id) => All.Where(card => card.Ministry.ParentId == id);

    public IReadOnlyList<MinistryCard> Above(string id)
    {
        List<MinistryCard> above = [];
        for (MinistryCard? next = Find(id) is { Ministry.ParentId: { Length: > 0 } parent } ? Find(parent) : null;
             next is not null && above.All(seen => seen.Ministry.Id != next.Ministry.Id) && next.Ministry.Id != id;
             next = next.Ministry.ParentId.Length > 0 ? Find(next.Ministry.ParentId) : null)
            above.Add(next);
        return above;
    }

    public IReadOnlyList<MinistryCard> Below(string id)
    {
        List<MinistryCard> below = [];
        Queue<string> waiting = new([id]);
        while (waiting.TryDequeue(out string? next))
            foreach (MinistryCard child in ChildrenOf(next))
                if (child.Ministry.Id != id && below.All(seen => seen.Ministry.Id != child.Ministry.Id))
                {
                    below.Add(child);
                    waiting.Enqueue(child.Ministry.Id);
                }
        return below;
    }

    public List<string> Showing(string id, string part) =>
        Find(id) is { } card && MinistryLogic.IsOn(card.Ministry, part)
            ? [id, .. Above(id).Where(place => MinistryLogic.IsOn(place.Ministry, part)).Select(place => place.Ministry.Id)]
            : [];

    public IReadOnlyList<string> Gathered(string id, string part)
    {
        if (Find(id) is not { } card || !MinistryLogic.IsOn(card.Ministry, part))
            return [];
        if (!card.Ministry.PartsFromBelow.Contains(part))
            return [id];
        return [id, .. Below(id).Where(below => MinistryLogic.IsOn(below.Ministry, part) && Reaches(below, id, part)).Select(below => below.Ministry.Id)];
    }

    private bool Reaches(MinistryCard below, string top, string part) =>
        Above(below.Ministry.Id).TakeWhile(above => above.Ministry.Id != top).All(above => MinistryLogic.IsOn(above.Ministry, part));
}
