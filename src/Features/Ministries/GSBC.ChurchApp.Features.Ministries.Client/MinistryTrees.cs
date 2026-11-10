using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class MinistryTrees
{
    public static IEnumerable<(MinistrySummary Ministry, int Depth)> Walk(IReadOnlyList<MinistrySummary> ministries, string leaveOut = "", int deepest = int.MaxValue)
    {
        ILookup<string, MinistrySummary> children = ministries.ToLookup(ministry => ministry.ParentId);
        Stack<(MinistrySummary, int)> waiting = new(children[""].Reverse().Select(ministry => (ministry, 0)));
        HashSet<string> seen = [];
        while (waiting.TryPop(out (MinistrySummary Ministry, int Depth) next))
        {
            if (next.Ministry.Id == leaveOut || !seen.Add(next.Ministry.Id))
                continue;
            yield return next;
            if (next.Depth < deepest)
                foreach (MinistrySummary child in children[next.Ministry.Id].Reverse())
                    waiting.Push((child, next.Depth + 1));
        }
    }

    public static int Under(IReadOnlyList<MinistrySummary> ministries, MinistrySummary ministry) =>
        ministries.Count(child => child.ParentId == ministry.Id);
}
