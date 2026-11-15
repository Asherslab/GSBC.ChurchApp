using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal static class MapCells
{
    public static string? Problem(IReadOnlyCollection<CellRef> on, IReadOnlyCollection<CellRef> known, RegionChange change)
    {
        HashSet<CellRef> staying = on.Except(change.Off).ToHashSet();
        HashSet<CellRef> added = change.On.Except(on).ToHashSet();
        if (staying.Count + added.Count == 0)
            return "An area needs at least one region. Delete the area instead.";
        if (known.Union(added).Count() > RegionGrid.MostCells)
            return $"An area holds at most {RegionGrid.MostCells} regions. Add another area for somewhere further away.";
        return Reachable(staying, added) ? null : "A region you add must touch one that stays on.";
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
