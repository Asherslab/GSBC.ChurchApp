using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal sealed class MapSquares(OutreachMapsDbContext db, AreaBuilder builder)
{
    public async Task SwitchOnAsync(string areaId, IReadOnlyCollection<MapCellRecord> cells, CancellationToken cancellationToken)
    {
        List<string> ids = [.. cells.Select(cell => cell.SquareId)];
        Dictionary<string, MapSquareRecord> known = await db.Squares.Where(square => ids.Contains(square.Id)).ToDictionaryAsync(square => square.Id, cancellationToken);
        HashSet<string> shared = await OnElsewhereAsync(areaId, ids, cancellationToken);
        foreach (MapCellRecord cell in cells)
        {
            if (!known.TryGetValue(cell.SquareId, out MapSquareRecord? square))
            {
                square = new MapSquareRecord { Id = cell.SquareId, Row = cell.Row, Column = cell.Column };
                db.Squares.Add(square);
                known[square.Id] = square;
            }
            else if (shared.Contains(square.Id))
                continue;
            (square.Pending, square.Empty) = (true, false);
        }
    }

    public async Task<HashSet<string>> OnElsewhereAsync(string areaId, IReadOnlyCollection<string> squareIds, CancellationToken cancellationToken) =>
        [.. await db.Cells.Where(cell => cell.AreaId != areaId && cell.On && squareIds.Contains(cell.SquareId)).Select(cell => cell.SquareId).Distinct().ToListAsync(cancellationToken)];

    public async Task LetGoAsync(IReadOnlyCollection<string> squareIds, CancellationToken cancellationToken)
    {
        HashSet<string> stillOn = [.. await db.Cells.Where(cell => cell.On && squareIds.Contains(cell.SquareId)).Select(cell => cell.SquareId).Distinct().ToListAsync(cancellationToken)];
        HashSet<string> gone = [.. squareIds.Where(id => !stillOn.Contains(id))];
        await db.Squares.Where(square => gone.Contains(square.Id)).ExecuteUpdateAsync(set => set.SetProperty(square => square.Pending, false).SetProperty(square => square.Empty, false), cancellationToken);
        await builder.RemoveAsync(gone, cancellationToken);
        await builder.ForgetOldFilesAsync(cancellationToken);
    }

    public async Task<Func<string, string?, List<string>>> AlsoInAsync(CancellationToken cancellationToken)
    {
        ILookup<string, (string Id, string Name)> on = (await db.Cells.AsNoTracking().Where(cell => cell.On)
                .Join(db.Areas.Where(area => area.DeletedAt == null), cell => cell.AreaId, area => area.Id, (cell, area) => new { cell.SquareId, area.Id, area.Name, area.CreatedAt })
                .OrderBy(found => found.CreatedAt).ToListAsync(cancellationToken))
            .ToLookup(found => found.SquareId, found => (found.Id, found.Name));
        return (squareId, exceptArea) => [.. on[squareId].Where(area => area.Id != exceptArea).Select(area => area.Name)];
    }

    public async Task<bool> QueueStrandedAsync(CancellationToken cancellationToken)
    {
        List<string> pending = await db.Cells.Where(cell => cell.On && db.Squares.Any(square => square.Id == cell.SquareId && square.Pending))
            .Select(cell => cell.AreaId).Distinct().ToListAsync(cancellationToken);
        return await db.Areas.Where(area => pending.Contains(area.Id) && area.DeletedAt == null && area.State == MapAreaStates.Built)
            .ExecuteUpdateAsync(set => set.SetProperty(area => area.State, MapAreaStates.Waiting), cancellationToken) > 0;
    }
}
