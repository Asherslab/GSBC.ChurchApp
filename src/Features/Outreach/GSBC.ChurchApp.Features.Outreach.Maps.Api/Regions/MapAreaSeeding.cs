using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal static class MapAreaSeeding
{
    private const string ChurchAreaId = "church";
    private const string ChurchAreaName = "Around the church";

    public static async Task SeedAsync(IServiceScopeFactory scopes, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        OutreachMapsDbContext db = scope.ServiceProvider.GetRequiredService<OutreachMapsDbContext>();
        await GiveGridsAsync(db, cancellationToken);
        DateTime now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        await ShareGridAsync(db, scope.ServiceProvider.GetRequiredService<MapSquares>(), now, cancellationToken);
        MapSourcesOptions options = scope.ServiceProvider.GetRequiredService<IOptions<MapSourcesOptions>>().Value;
        if (!options.SeedChurchArea || await db.Areas.AnyAsync(cancellationToken))
            return;
        (MapGrid grid, List<CellRef> planned) = RegionGrid.Plan(new GeoPoint(options.ChurchLatitude, options.ChurchLongitude), options.ChurchRadiusKm * 1000);
        (MapAreaRecord area, List<MapCellRecord> cells) = MapAreas.New(ChurchAreaId, ChurchAreaName, ChurchAreaId, grid, planned, now);
        db.Areas.Add(area);
        db.Cells.AddRange(cells);
        db.Squares.AddRange(cells.Select(cell => new MapSquareRecord { Id = cell.SquareId, Row = cell.Row, Column = cell.Column, Pending = true }));
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task ShareGridAsync(OutreachMapsDbContext db, MapSquares squares, DateTime now, CancellationToken cancellationToken)
    {
        List<MapAreaRecord> areas = await db.Areas.Where(area => area.DeletedAt == null).OrderBy(area => area.CreatedAt).ToListAsync(cancellationToken);
        if (areas is not [var oldest, ..] || areas.All(area => area.GridId == oldest.GridId))
            return;
        MapGrid shared = RegionGrid.GridOf(oldest);
        List<string> left = [];
        foreach (MapAreaRecord area in areas.Where(area => area.GridId != oldest.GridId))
        {
            List<MapCellRecord> old = await db.Cells.Where(cell => cell.AreaId == area.Id).ToListAsync(cancellationToken);
            MapGrid own = RegionGrid.GridOf(area);
            List<CellRef> on = [.. old.Where(cell => cell.On).Select(cell => new CellRef { Row = cell.Row, Column = cell.Column })];
            List<CellRef> cells = RegionGrid.SameLines(own, shared)
                ? on
                : RegionGrid.Plan(RegionGrid.Union(on.Select(cell => RegionGrid.Core(own, cell.Row, cell.Column))), shared).Cells;
            left.AddRange(old.Select(cell => cell.SquareId));
            db.Cells.RemoveRange(old);
            await db.SaveChangesAsync(cancellationToken);

            area.GridId = oldest.GridId;
            RegionGrid.SetGrid(area, shared);
            List<MapCellRecord> moved = [.. cells.Select(cell => MapAreas.Cell(area, cell))];
            db.Cells.AddRange(moved);
            MapAreas.Fit(area, moved);
            await squares.SwitchOnAsync(area.Id, moved, cancellationToken);
            (area.State, area.Problem, area.Stage, area.StepsDone, area.Steps) = (MapAreaStates.Waiting, null, null, 0, 0);
            db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, now));
            await db.SaveChangesAsync(cancellationToken);
        }
        await squares.LetGoAsync(left, cancellationToken);
        await db.Squares.Where(square => left.Contains(square.Id)).ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task GiveGridsAsync(OutreachMapsDbContext db, CancellationToken cancellationToken)
    {
        List<MapAreaRecord> areas = await db.Areas.Where(area => area.CellLatitude == 0 && area.DeletedAt == null).ToListAsync(cancellationToken);
        foreach (MapAreaRecord area in areas)
        {
            (MapGrid grid, List<CellRef> cells) = RegionGrid.Plan(new GeoBounds(area.South, area.West, area.North, area.East));
            RegionGrid.SetGrid(area, grid);
            area.GridId = area.Id;
            Dictionary<string, RegionRecord> built = await db.Regions.AsNoTracking().Where(region => region.GridId == area.Id).ToDictionaryAsync(region => region.Id, cancellationToken);
            foreach (CellRef cell in cells)
            {
                string id = RegionGrid.RegionId(area.Id, cell.Row, cell.Column);
                db.Cells.Add(new MapCellRecord { AreaId = area.Id, SquareId = id, Row = cell.Row, Column = cell.Column, On = true });
                db.Squares.Add(new MapSquareRecord
                {
                    Id = id, Row = cell.Row, Column = cell.Column,
                    Pending = !built.ContainsKey(id) && area.State != MapAreaStates.Built,
                    Empty = !built.ContainsKey(id) && area.State == MapAreaStates.Built,
                    Version = built.GetValueOrDefault(id)?.Version ?? 0,
                    Name = built.GetValueOrDefault(id)?.Name ?? "",
                });
            }
        }
        if (areas.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
