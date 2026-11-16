using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Places;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api;

internal sealed class OutreachMapsService(
    OutreachMapsDbContext db,
    AreaBuildRunner runner,
    AreaBuilder builder,
    PlaceIndex places,
    RegionEstimates estimates,
    OverviewMap overview,
    IEnumerable<IRegionUses> uses,
    IBlobStore blobs,
    TimeProvider time) : IOutreachMapsService
{
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async ValueTask<RegionList> RegionsAsync(RegionListRequest request, CallContext context = default) => new()
    {
        Regions = (await db.Regions.AsNoTracking().OrderBy(region => region.Id).ToListAsync(context.CancellationToken))
            .Select(region => RegionFeed.ToMessage(region, blobs)).ToList(),
    };

    public async ValueTask<MapOverview> OverviewAsync(MapOverviewRequest request, CallContext context = default) =>
        await overview.MessageAsync(context.CancellationToken);

    public async ValueTask<MapAreaList> AreasAsync(MapAreaListRequest request, CallContext context = default) => await ListAsync(context.CancellationToken);

    public async ValueTask<MapArea> AddAsync(AddMapArea request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        string name = request.Name.Trim();
        if (name.Length == 0)
            throw Invalid("Give the area a name.");
        CheckRadius(request.RadiusKm);
        GeoPoint centre = Centre(request.CentreLatitude, request.CentreLongitude);
        HashSet<string> taken = [.. await db.Areas.Select(area => area.Id).ToListAsync(cancellationToken), .. MapAreas.Reserved];
        (MapGrid grid, List<CellRef> planned) = await PlanAsync(centre, request.RadiusKm * 1000, cancellationToken);
        if (planned.Count == 0)
            throw Invalid("Every region there is already in another area.");
        (MapAreaRecord added, List<MapCellRecord> cells) = MapAreas.New(MapAreas.Slug(name, taken), name, grid, planned, Now);
        db.Areas.Add(added);
        db.Cells.AddRange(cells);
        db.FeedChanges.Add(RegionFeed.BuildChanged(added.Id, Now));
        await db.SaveChangesAsync(cancellationToken);
        runner.Wake();
        return MapAreas.ToMessage(added, cells, [], blobs);
    }

    public async ValueTask<MapArea> RenameAsync(RenameMapArea request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MapAreaRecord area = await FindAsync(request.Id, cancellationToken);
        string name = request.Name.Trim();
        if (name.Length == 0)
            throw Invalid("Give the area a name.");
        area.Name = name;
        db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, Now));
        await db.SaveChangesAsync(cancellationToken);
        return await MessageAsync(area, cancellationToken);
    }

    public async ValueTask<MapArea> RebuildAsync(MapAreaRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MapAreaRecord area = await FindAsync(request.Id, cancellationToken);
        await db.Cells.Where(cell => cell.AreaId == area.Id && cell.On).ExecuteUpdateAsync(set => set.SetProperty(cell => cell.Pending, true), cancellationToken);
        return await QueueAsync(area, cancellationToken);
    }

    public async ValueTask<MapArea> RetryAsync(MapAreaRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MapAreaRecord area = await FindAsync(request.Id, cancellationToken);
        return await QueueAsync(area, cancellationToken);
    }

    public async ValueTask<MapAreaList> DeleteAsync(MapAreaRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MapAreaRecord area = await FindAsync(request.Id, cancellationToken);
        if (area.State == MapAreaStates.Building)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Its maps are being built. Delete it once that finishes."));
        area.DeletedAt = Now;
        db.Cells.RemoveRange(await db.Cells.Where(cell => cell.AreaId == area.Id).ToListAsync(cancellationToken));
        db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, Now));
        await builder.RemoveAsync((await db.Regions.Where(region => region.AreaId == area.Id).Select(region => region.Id).ToListAsync(cancellationToken)).ToHashSet(), cancellationToken);
        await builder.ForgetOldFilesAsync(cancellationToken);
        return await ListAsync(cancellationToken);
    }

    public async ValueTask<PlaceList> FindPlacesAsync(PlaceSearch request, CallContext context = default) =>
        await places.FindAsync(request.Text, context.CancellationToken);

    public async ValueTask<MapAreaPreview> PreviewAsync(MapAreaPlan request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        CheckRadius(request.RadiusKm);
        GeoPoint centre = Centre(request.CentreLatitude, request.CentreLongitude);
        (MapGrid grid, List<CellRef> cells) = await PlanAsync(centre, request.RadiusKm * 1000, cancellationToken);
        GeoBounds reach = RegionGrid.Union(cells.Select(cell => RegionGrid.Core(grid, cell.Row, cell.Column)));
        return new MapAreaPreview
        {
            Grid = grid,
            Cells = await estimates.EstimateAsync(grid, cells, cancellationToken),
            Places = await places.WithinAsync(reach, cancellationToken),
        };
    }

    public async ValueTask<RegionChangeCheck> CheckChangeAsync(RegionChange request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MapAreaRecord area = await FindAsync(request.AreaId, cancellationToken);
        List<MapCellRecord> cells = await db.Cells.AsNoTracking().Where(cell => cell.AreaId == area.Id).ToListAsync(cancellationToken);
        RegionChange change = Effective(request, cells);
        HashSet<string> going = cells.Where(cell => cell.On && change.Off.Contains(Ref(cell))).Select(cell => cell.Id).ToHashSet();
        List<RegionRecord> regions = await db.Regions.AsNoTracking().Where(region => going.Contains(region.Id)).ToListAsync(cancellationToken);
        List<RegionUse> found = [];
        if (regions.Count > 0)
            foreach (IRegionUses source in uses)
                found.AddRange(await source.UsesAsync(regions.Select(region => region.Id).ToList(), cancellationToken));
        return new RegionChangeCheck
        {
            Problem = MapCells.Problem(OnCells(cells), cells.Select(Ref).ToList(), change, await ElsewhereAsync(area, cancellationToken)),
            Added = await estimates.EstimateAsync(RegionGrid.GridOf(area), change.On, cancellationToken),
            Uses = found,
            FreedBytes = regions.Sum(region => region.StreetsBytes + region.MapBytes),
        };
    }

    public async ValueTask<MapArea> ChangeRegionsAsync(RegionChange request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MapAreaRecord area = await FindAsync(request.AreaId, cancellationToken);
        List<MapCellRecord> cells = await db.Cells.Where(cell => cell.AreaId == area.Id).ToListAsync(cancellationToken);
        RegionChange change = Effective(request, cells);
        if (MapCells.Problem(OnCells(cells), cells.Select(Ref).ToList(), change, await ElsewhereAsync(area, cancellationToken)) is { } problem)
            throw Invalid(problem);
        if (change.Off.Count > 0 && area.State == MapAreaStates.Building)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Its maps are being built. Switch regions off once that finishes."));

        foreach (MapCellRecord cell in cells.Where(cell => change.Off.Contains(Ref(cell))))
            (cell.On, cell.Pending, cell.Empty) = (false, false, false);
        foreach (CellRef on in change.On)
        {
            MapCellRecord cell = cells.FirstOrDefault(known => Ref(known).Equals(on)) ?? Added(area, on);
            (cell.On, cell.Pending, cell.Empty) = (true, true, false);
        }
        MapAreas.Fit(area, cells);
        db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, Now));
        await db.SaveChangesAsync(cancellationToken);
        await builder.RemoveAsync(cells.Where(cell => change.Off.Contains(Ref(cell))).Select(cell => cell.Id).ToHashSet(), cancellationToken);
        await builder.ForgetOldFilesAsync(cancellationToken);
        return change.On.Count > 0 ? await QueueAsync(area, cancellationToken) : await MessageAsync(area, cancellationToken);

        MapCellRecord Added(MapAreaRecord to, CellRef at)
        {
            MapCellRecord cell = new() { Id = RegionGrid.RegionId(to.Id, at.Row, at.Column), AreaId = to.Id, Row = at.Row, Column = at.Column };
            db.Cells.Add(cell);
            cells.Add(cell);
            return cell;
        }
    }

    private async Task<(MapGrid Grid, List<CellRef> Cells)> PlanAsync(GeoPoint centre, double radiusMetres, CancellationToken cancellationToken)
    {
        List<MapAreaRecord> areas = await db.Areas.AsNoTracking().Where(area => area.DeletedAt == null).OrderBy(area => area.CreatedAt).ToListAsync(cancellationToken);
        if (areas.Count == 0)
            return RegionGrid.Plan(centre, radiusMetres);
        MapGrid shared = RegionGrid.GridOf(areas[0]);
        return RegionGrid.Plan(centre, radiusMetres, shared, await OnCellsAsync(areas.Where(area => RegionGrid.SameLines(RegionGrid.GridOf(area), shared)), cancellationToken));
    }

    private async Task<HashSet<CellRef>> ElsewhereAsync(MapAreaRecord area, CancellationToken cancellationToken)
    {
        MapGrid grid = RegionGrid.GridOf(area);
        List<MapAreaRecord> others = await db.Areas.AsNoTracking().Where(other => other.DeletedAt == null && other.Id != area.Id).ToListAsync(cancellationToken);
        return await OnCellsAsync(others.Where(other => RegionGrid.SameLines(RegionGrid.GridOf(other), grid)), cancellationToken);
    }

    private async Task<HashSet<CellRef>> OnCellsAsync(IEnumerable<MapAreaRecord> areas, CancellationToken cancellationToken)
    {
        List<string> ids = [.. areas.Select(area => area.Id)];
        return [.. (await db.Cells.AsNoTracking().Where(cell => ids.Contains(cell.AreaId) && cell.On).ToListAsync(cancellationToken)).Select(Ref)];
    }

    private static RegionChange Effective(RegionChange request, List<MapCellRecord> cells)
    {
        HashSet<CellRef> on = OnCells(cells).ToHashSet();
        return new RegionChange
        {
            AreaId = request.AreaId,
            Off = request.Off.Where(on.Contains).Distinct().ToList(),
            On = request.On.Where(cell => !on.Contains(cell)).Distinct().ToList(),
        };
    }

    private static List<CellRef> OnCells(IEnumerable<MapCellRecord> cells) => cells.Where(cell => cell.On).Select(Ref).ToList();

    private static CellRef Ref(MapCellRecord cell) => new() { Row = cell.Row, Column = cell.Column };

    private async Task<MapArea> QueueAsync(MapAreaRecord area, CancellationToken cancellationToken)
    {
        if (area.State is not (MapAreaStates.Waiting or MapAreaStates.Building))
            (area.State, area.Problem, area.Stage) = (MapAreaStates.Waiting, null, null);
        db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, Now));
        await db.SaveChangesAsync(cancellationToken);
        runner.Wake();
        return await MessageAsync(area, cancellationToken);
    }

    private async Task<MapAreaList> ListAsync(CancellationToken cancellationToken)
    {
        List<MapAreaRecord> areas = await db.Areas.AsNoTracking().Where(area => area.DeletedAt == null).OrderBy(area => area.CreatedAt).ToListAsync(cancellationToken);
        ILookup<string, RegionRecord> regions = (await db.Regions.AsNoTracking().ToListAsync(cancellationToken)).ToLookup(region => region.AreaId);
        ILookup<string, MapCellRecord> cells = (await db.Cells.AsNoTracking().ToListAsync(cancellationToken)).ToLookup(cell => cell.AreaId);
        return new MapAreaList { Areas = areas.Select(area => MapAreas.ToMessage(area, cells[area.Id], regions[area.Id], blobs)).ToList() };
    }

    private async Task<MapArea> MessageAsync(MapAreaRecord area, CancellationToken cancellationToken) =>
        MapAreas.ToMessage(area,
            await db.Cells.AsNoTracking().Where(cell => cell.AreaId == area.Id).ToListAsync(cancellationToken),
            await db.Regions.AsNoTracking().Where(region => region.AreaId == area.Id).ToListAsync(cancellationToken), blobs);

    private async Task<MapAreaRecord> FindAsync(string id, CancellationToken cancellationToken) =>
        await db.Areas.FirstOrDefaultAsync(area => area.Id == id && area.DeletedAt == null, cancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, "That area isn't here any more."));

    private static void CheckRadius(double radiusKm)
    {
        if (radiusKm is < MapAreas.SmallestRadiusKm or > MapAreas.LargestRadiusKm)
            throw Invalid($"An area reaches between {MapAreas.SmallestRadiusKm:0} and {MapAreas.LargestRadiusKm:0} km from its centre.");
    }

    private static GeoPoint Centre(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            throw Invalid("That centre isn't a place on the map.");
        return new GeoPoint(latitude, longitude);
    }

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}
