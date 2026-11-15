using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal static class MapCells
{
    public static string? Problem(IReadOnlyCollection<CellRef> on, IReadOnlyCollection<CellRef> known, RegionChange change, IReadOnlySet<CellRef>? elsewhere = null)
    {
        HashSet<CellRef> staying = on.Except(change.Off).ToHashSet();
        HashSet<CellRef> added = change.On.Except(on).ToHashSet();
        if (elsewhere is not null && added.Any(elsewhere.Contains))
            return "A region you add is already in another area. Switch it off there first.";
        if (staying.Count + added.Count == 0)
            return "An area needs at least one region. Delete the area instead.";
        if (known.Union(added).Count() > RegionGrid.MostCells)
            return $"An area holds at most {RegionGrid.MostCells} regions. Add another area for somewhere further away.";
        return Reachable(staying, added) ? null : "A region you add must touch one that stays on.";
    }

    public static List<List<T>> Groups<T>(IReadOnlyList<T> cells, Func<T, CellRef> at)
    {
        List<List<T>> groups = [];
        HashSet<int> placed = [];
        for (int first = 0; first < cells.Count; first++)
        {
            if (!placed.Add(first))
                continue;
            List<T> group = [cells[first]];
            Queue<int> next = new([first]);
            while (next.TryDequeue(out int from))
                for (int other = 0; other < cells.Count; other++)
                    if (!placed.Contains(other) && MapGrid.Touching(at(cells[from]), at(cells[other])))
                    {
                        placed.Add(other);
                        group.Add(cells[other]);
                        next.Enqueue(other);
                    }
            groups.Add(group);
        }
        return groups;
    }

    private static bool Reachable(HashSet<CellRef> staying, HashSet<CellRef> added)
    {
        HashSet<CellRef> reached = [];
        Queue<CellRef> next = new(staying);
        while (next.TryDequeue(out CellRef? from))
            foreach (CellRef cell in added.Where(cell => !reached.Contains(cell) && MapGrid.Touching(from, cell)))
            {
                reached.Add(cell);
                next.Enqueue(cell);
            }
        return reached.Count == added.Count;
    }
}
