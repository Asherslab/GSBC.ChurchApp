using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class MinistryTrees
{
    public static IEnumerable<(MinistrySummary Ministry, int Depth)> Walk(IReadOnlyList<MinistrySummary> ministries, int deepest = int.MaxValue, IReadOnlySet<string>? folded = null)
    {
        ILookup<string, MinistrySummary> children = ministries.ToLookup(ministry => ministry.ParentId);
        Stack<(MinistrySummary, int)> waiting = new(children[""].Reverse().Select(ministry => (ministry, 0)));
        HashSet<string> seen = [];
        while (waiting.TryPop(out (MinistrySummary Ministry, int Depth) next))
        {
            if (!seen.Add(next.Ministry.Id))
                continue;
            yield return next;
            if (next.Depth < deepest && folded?.Contains(next.Ministry.Id) != true)
                foreach (MinistrySummary child in children[next.Ministry.Id].Reverse())
                    waiting.Push((child, next.Depth + 1));
        }
    }

    public static IReadOnlySet<string> Above(IReadOnlyList<MinistrySummary> ministries, string? id)
    {
        Dictionary<string, MinistrySummary> byId = ministries.ToDictionary(ministry => ministry.Id);
        HashSet<string> above = [];
        while (id is not null && byId.TryGetValue(id, out MinistrySummary? ministry) && ministry.ParentId.Length > 0 && above.Add(ministry.ParentId))
            id = ministry.ParentId;
        return above;
    }

    public static int Under(IReadOnlyList<MinistrySummary> ministries, MinistrySummary ministry) =>
        ministries.Count(child => child.ParentId == ministry.Id);
}
