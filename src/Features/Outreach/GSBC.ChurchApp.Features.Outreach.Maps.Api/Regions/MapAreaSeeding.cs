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
        MapSourcesOptions options = scope.ServiceProvider.GetRequiredService<IOptions<MapSourcesOptions>>().Value;
        if (!options.SeedChurchArea || await db.Areas.AnyAsync(cancellationToken))
            return;
        DateTime now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        (MapAreaRecord area, List<MapCellRecord> cells) = MapAreas.New(ChurchAreaId, ChurchAreaName, new GeoPoint(options.ChurchLatitude, options.ChurchLongitude), options.ChurchRadiusKm * 1000, now);
        db.Areas.Add(area);
        db.Cells.AddRange(cells);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task GiveGridsAsync(OutreachMapsDbContext db, CancellationToken cancellationToken)
    {
        List<MapAreaRecord> areas = await db.Areas.Where(area => area.CellLatitude == 0 && area.DeletedAt == null).ToListAsync(cancellationToken);
        foreach (MapAreaRecord area in areas)
        {
            (MapGrid grid, List<CellRef> cells) = RegionGrid.Plan(new GeoBounds(area.South, area.West, area.North, area.East));
            RegionGrid.SetGrid(area, grid);
            Dictionary<string, RegionRecord> built = await db.Regions.AsNoTracking().Where(region => region.AreaId == area.Id).ToDictionaryAsync(region => region.Id, cancellationToken);
            db.Cells.AddRange(cells.Select(cell =>
            {
                string id = RegionGrid.RegionId(area.Id, cell.Row, cell.Column);
                return new MapCellRecord
                {
                    Id = id, AreaId = area.Id, Row = cell.Row, Column = cell.Column, On = true,
                    Pending = !built.ContainsKey(id) && area.State != MapAreaStates.Built,
                    Empty = !built.ContainsKey(id) && area.State == MapAreaStates.Built,
                    Version = built.GetValueOrDefault(id)?.Version ?? 0,
                    Name = built.GetValueOrDefault(id)?.Name ?? "",
                };
            }));
        }
        if (areas.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
