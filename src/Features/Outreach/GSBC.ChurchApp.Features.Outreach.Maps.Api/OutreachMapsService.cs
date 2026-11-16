using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Places;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Storage;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api;

internal sealed class OutreachMapsService(
    OutreachMapsDbContext db,
    AreaBuildRunner runner,
    PlaceIndex places,
    RegionEstimates estimates,
    MapSquares squares,
    OverviewMap overview,
    IEnumerable<IRegionUses> uses,
    IBlobStore blobs,
    IOptions<MapSourcesOptions> options,
    TimeProvider time) : IOutreachMapsService
{
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async ValueTask<RegionList> RegionsAsync(RegionListRequest request, CallContext context = default) => new()
    {
        Regions = (await db.Regions.AsNoTracking().OrderBy(region => region.Id).ToListAsync(context.CancellationToken))
            .Select(region => RegionFeed.ToMessage(region, blobs)).ToList(),
        ChurchLatitude = options.Value.ChurchLatitude,
        ChurchLongitude = options.Value.ChurchLongitude,
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
        (MapGrid grid, string? gridId, List<CellRef> planned) = await PlanAsync(centre, request.RadiusKm * 1000, cancellationToken);
        string id = MapAreas.Slug(name, taken);
        (MapAreaRecord added, List<MapCellRecord> cells) = MapAreas.New(id, name, gridId ?? id, grid, planned, Now);
        db.Areas.Add(added);
        db.Cells.AddRange(cells);
        await squares.SwitchOnAsync(added.Id, cells, cancellationToken);
        db.FeedChanges.Add(RegionFeed.BuildChanged(added.Id, Now));
        await db.SaveChangesAsync(cancellationToken);
        runner.Wake();
        return await MessageAsync(added, cancellationToken);
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
        List<string> on = await db.Cells.Where(cell => cell.AreaId == area.Id && cell.On).Select(cell => cell.SquareId).ToListAsync(cancellationToken);
        await db.Squares.Where(square => on.Contains(square.Id)).ExecuteUpdateAsync(set => set.SetProperty(square => square.Pending, true), cancellationToken);
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
        List<MapCellRecord> cells = await db.Cells.Where(cell => cell.AreaId == area.Id).ToListAsync(cancellationToken);
        db.Cells.RemoveRange(cells);
        db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, Now));
        await db.SaveChangesAsync(cancellationToken);
        await squares.LetGoAsync([.. cells.Select(cell => cell.SquareId)], cancellationToken);
        await WakeStrandedAsync(cancellationToken);
        return await ListAsync(cancellationToken);
    }

    public async ValueTask<PlaceList> FindPlacesAsync(PlaceSearch request, CallContext context = default) =>
        await places.FindAsync(request.Text, context.CancellationToken);

    public async ValueTask<MapAreaPreview> PreviewAsync(MapAreaPlan request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        CheckRadius(request.RadiusKm);
        GeoPoint centre = Centre(request.CentreLatitude, request.CentreLongitude);
        (MapGrid grid, string? gridId, List<CellRef> cells) = await PlanAsync(centre, request.RadiusKm * 1000, cancellationToken);
        GeoBounds reach = RegionGrid.Union(cells.Select(cell => RegionGrid.Core(grid, cell.Row, cell.Column)));
        return new MapAreaPreview
        {
            Grid = grid,
            Cells = await EstimateAsync(grid, gridId, null, cells, cancellationToken),
            Places = await places.WithinAsync(reach, cancellationToken),
        };
    }

    public async ValueTask<RegionChangeCheck> CheckChangeAsync(RegionChange request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MapAreaRecord area = await FindAsync(request.AreaId, cancellationToken);
        List<MapCellRecord> cells = await db.Cells.AsNoTracking().Where(cell => cell.AreaId == area.Id).ToListAsync(cancellationToken);
        RegionChange change = Effective(request, cells);
        List<string> off = [.. cells.Where(cell => cell.On && change.Off.Contains(Ref(cell))).Select(cell => cell.SquareId)];
        HashSet<string> shared = await squares.OnElsewhereAsync(area.Id, off, cancellationToken);
        HashSet<string> going = [.. off.Where(id => !shared.Contains(id))];
        List<RegionRecord> regions = await db.Regions.AsNoTracking().Where(region => going.Contains(region.Id)).ToListAsync(cancellationToken);
        List<RegionUse> found = [];
        if (regions.Count > 0)
            foreach (IRegionUses source in uses)
                found.AddRange(await source.UsesAsync(regions.Select(region => region.Id).ToList(), cancellationToken));
        return new RegionChangeCheck
        {
            Problem = MapCells.Problem(OnCells(cells), cells.Select(Ref).ToList(), change),
            Added = await EstimateAsync(RegionGrid.GridOf(area), area.GridId, area.Id, change.On, cancellationToken),
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
        if (MapCells.Problem(OnCells(cells), cells.Select(Ref).ToList(), change) is { } problem)
            throw Invalid(problem);
        if (change.Off.Count > 0 && area.State == MapAreaStates.Building)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Its maps are being built. Switch regions off once that finishes."));

        List<MapCellRecord> switchedOff = [.. cells.Where(cell => change.Off.Contains(Ref(cell)))];
        foreach (MapCellRecord cell in switchedOff)
            cell.On = false;
        List<MapCellRecord> switchedOn = [];
        foreach (CellRef on in change.On)
        {
            MapCellRecord cell = cells.FirstOrDefault(known => Ref(known).Equals(on)) ?? Added(on);
            cell.On = true;
            switchedOn.Add(cell);
        }
        MapAreas.Fit(area, cells);
        await squares.SwitchOnAsync(area.Id, switchedOn, cancellationToken);
        db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, Now));
        await db.SaveChangesAsync(cancellationToken);
        await squares.LetGoAsync([.. switchedOff.Select(cell => cell.SquareId)], cancellationToken);
        if (change.On.Count > 0)
            return await QueueAsync(area, cancellationToken);
        await WakeStrandedAsync(cancellationToken);
        return await MessageAsync(area, cancellationToken);

        MapCellRecord Added(CellRef at)
        {
            MapCellRecord cell = MapAreas.Cell(area, at);
            db.Cells.Add(cell);
            cells.Add(cell);
            return cell;
        }
    }

    private async Task<(MapGrid Grid, string? GridId, List<CellRef> Cells)> PlanAsync(GeoPoint centre, double radiusMetres, CancellationToken cancellationToken)
    {
        MapAreaRecord? oldest = await db.Areas.AsNoTracking().Where(area => area.DeletedAt == null).OrderBy(area => area.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (oldest is null)
        {
            (MapGrid grid, List<CellRef> cells) = RegionGrid.Plan(centre, radiusMetres);
            return (grid, null, cells);
        }
        (MapGrid shared, List<CellRef> reached) = RegionGrid.Plan(centre, radiusMetres, RegionGrid.GridOf(oldest));
        return (shared, oldest.GridId, reached);
    }

    private async Task<List<CellEstimate>> EstimateAsync(MapGrid grid, string? gridId, string? exceptArea, IReadOnlyCollection<CellRef> cells, CancellationToken cancellationToken)
    {
        if (gridId is null)
            return await estimates.EstimateAsync(grid, cells, cancellationToken);
        Func<string, string?, List<string>> alsoIn = await squares.AlsoInAsync(cancellationToken);
        Dictionary<CellRef, string> ids = cells.ToDictionary(cell => cell, cell => RegionGrid.RegionId(gridId, cell.Row, cell.Column));
        List<string> wanted = [.. ids.Values];
        Dictionary<string, RegionRecord> built = await db.Regions.AsNoTracking().Where(region => wanted.Contains(region.Id)).ToDictionaryAsync(region => region.Id, cancellationToken);
        HashSet<string> empty = [.. await db.Squares.AsNoTracking().Where(square => wanted.Contains(square.Id) && square.Empty && !square.Pending).Select(square => square.Id).ToListAsync(cancellationToken)];
        List<CellRef> unknown = [.. cells.Where(cell => !built.ContainsKey(ids[cell]) && !empty.Contains(ids[cell]))];
        Dictionary<CellRef, CellEstimate> counted = (await estimates.EstimateAsync(grid, unknown, cancellationToken)).ToDictionary(estimate => new CellRef { Row = estimate.Row, Column = estimate.Column });
        return [.. cells.Select(cell =>
        {
            CellEstimate estimate = counted.GetValueOrDefault(cell) ?? new CellEstimate
            {
                Row = cell.Row, Column = cell.Column,
                Addresses = built.GetValueOrDefault(ids[cell])?.Letterboxes ?? 0,
                Bytes = built.GetValueOrDefault(ids[cell]) is { } region ? region.StreetsBytes + region.MapBytes : 0,
            };
            estimate.AlsoIn = alsoIn(ids[cell], exceptArea);
            return estimate;
        })];
    }

    private async Task WakeStrandedAsync(CancellationToken cancellationToken)
    {
        if (await squares.QueueStrandedAsync(cancellationToken))
            runner.Wake();
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

    private async Task<MapAreaList> ListAsync(CancellationToken cancellationToken, string? only = null)
    {
        List<MapAreaRecord> areas = await db.Areas.AsNoTracking().Where(area => area.DeletedAt == null && (only == null || area.Id == only))
            .OrderBy(area => area.CreatedAt).ToListAsync(cancellationToken);
        List<string> ids = [.. areas.Select(area => area.Id)];
        ILookup<string, MapCellRecord> cells = (await db.Cells.AsNoTracking().Where(cell => ids.Contains(cell.AreaId)).ToListAsync(cancellationToken)).ToLookup(cell => cell.AreaId);
        List<string> squareIds = [.. cells.SelectMany(cell => cell).Select(cell => cell.SquareId).Distinct()];
        Dictionary<string, MapSquareRecord> known = await db.Squares.AsNoTracking().Where(square => squareIds.Contains(square.Id)).ToDictionaryAsync(square => square.Id, cancellationToken);
        Dictionary<string, RegionRecord> regions = await db.Regions.AsNoTracking().Where(region => squareIds.Contains(region.Id)).ToDictionaryAsync(region => region.Id, cancellationToken);
        Func<string, string?, List<string>> alsoIn = await squares.AlsoInAsync(cancellationToken);
        return new MapAreaList
        {
            Areas = [.. areas.Select(area => MapAreas.ToMessage(area, [.. cells[area.Id]], known, regions, square => alsoIn(square, area.Id), blobs))],
        };
    }

    private async Task<MapArea> MessageAsync(MapAreaRecord area, CancellationToken cancellationToken) =>
        (await ListAsync(cancellationToken, area.Id)).Areas.Single();

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
