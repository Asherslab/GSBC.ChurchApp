using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Ui.Maps;
using MapAreaShape = GSBC.ChurchApp.Platform.Ui.Maps.MapArea;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public sealed class OutreachAtlas
{
    private const double RoomToPan = 0.05;
    private const double AroundChurch = 0.025;
    private static readonly IReadOnlySet<CellRef> NothingChanged = new HashSet<CellRef>();
    private readonly Placed[] _placed;

    private readonly MapPoint? _church;

    public OutreachAtlas(IReadOnlyList<OutreachRegion> regions, IReadOnlyList<Maps.Contracts.MapArea> areas, MapOverview? overview = null, MapPoint? church = null)
    {
        _church = church;
        Overview = overview is { Map.Url.Length: > 0 } ? overview : null;
        Regions = [.. regions.Where(region => region.Map.Url.Length > 0).OrderBy(region => region.Id, StringComparer.Ordinal)];
        _placed = [.. Regions.Select(Placed.Of)];
        List<AreaCell> unbuilt = [.. areas.SelectMany(area => MapAreaDrawing.Cells(area, NothingChanged)
            .Where(cell => cell.Look is CellLook.Waiting or CellLook.Off or CellLook.Empty)
            .Select(cell => cell with { Name = Note(area, cell) }))];
        List<MapFrame> covered = [.. Regions.Select(region => Box(region.Core))];
        MapReach built = new(covered, [], []);
        List<MapMarker> notes = [.. unbuilt.Select(cell => built.FreeSpot(Box(cell.Core)) is { } spot ? MapAreaDrawing.Mark(cell.Core, spot, cell.Name) : null).OfType<MapMarker>()];
        IReadOnlyList<MapAreaShape> others = MapAreaDrawing.ShapesOutside(unbuilt, built);
        IReadOnlyList<MapPoint>? addable = Overview is { } shown ? [.. shown.Addable.Select(corner => new MapPoint(corner.Latitude, corner.Longitude))] : null;
        Reach = new MapReach(covered, others, notes, Addable: addable);
        MapFrame limit = Overview is { } whole ? Box(whole.Bounds) : Padded(MapFrame.Around(covered.Concat(unbuilt.Select(cell => Box(cell.Core))).SelectMany(MapReach.Ring)));
        Limits = new MapReach(covered, others, notes, limit, addable);
    }

    public MapOverview? Overview { get; }

    public MapBackdrop? Backdrop => Overview is { } overview ? new MapBackdrop(overview.Map.Url, Box(overview.Bounds), overview.MaxZoom) : null;

    public IReadOnlyList<OutreachRegion> Regions { get; }

    public MapReach Reach { get; }

    public MapReach Limits { get; }

    public MapSource? Source => Regions.Count > 0 ? MapSource.Urls(Regions.Select(region => region.Map.Url), Regions[0].Attribution)
        : Overview is { } overview ? MapSource.Urls([], overview.Attribution)
        : null;

    public MapFrame Opening
    {
        get
        {
            if (Regions.Count == 0 && Reach.Addable is { Count: > 0 } addable)
                return MapFrame.Around(addable, padding: 24);
            MapFrame all = Around(Regions);
            (double lat, double lon) = _church is { } church ? (church.Lat, church.Lon) : ((all.South + all.North) / 2, (all.West + all.East) / 2);
            return all.IsEmpty ? all : new MapFrame(lat - AroundChurch, lon - AroundChurch, lat + AroundChurch, lon + AroundChurch, 24, 15);
        }
    }

    public static MapFrame Around(IEnumerable<OutreachRegion> regions) =>
        MapFrame.Around(regions.SelectMany(region => MapReach.Ring(Box(region.Core))), padding: 24, maxZoom: 15);

    public OutreachRegion? RegionAt(MapPoint point) => RegionAt(_placed, point);

    public Func<MapPoint, bool> Owns(OutreachRegion region)
    {
        Placed own = Placed.Of(region);
        MapFrame reach = new(Math.Min(own.Core.South, own.Bounds.South), Math.Min(own.Core.West, own.Bounds.West),
            Math.Max(own.Core.North, own.Bounds.North), Math.Max(own.Core.East, own.Bounds.East));
        Placed[] near = [.. _placed.Where(other => Touch(other.Core, reach) || Touch(other.Bounds, reach))];
        return point => RegionAt(near, point)?.Id == region.Id;
    }

    public IEnumerable<OutreachRegion> In(MapFrame view) =>
        Regions.Where(region => region.Core.South < view.North && region.Core.North > view.South && region.Core.West < view.East && region.Core.East > view.West);

    public static async Task<OutreachAtlas> LoadAsync(OutreachMapsClient maps, IAccess access)
    {
        Task<RegionList> regions = maps.RegionsAsync();
        Task<MapAreaList> areas = access.Can(OutreachMapsPermissions.Manage) ? maps.AreasAsync() : Task.FromResult(new MapAreaList());
        Task<MapOverview> overview = maps.OverviewAsync();
        await Task.WhenAll(regions, areas, overview);
        RegionList list = await regions;
        MapPoint? church = list is { ChurchLatitude: 0, ChurchLongitude: 0 } ? null : new MapPoint(list.ChurchLatitude, list.ChurchLongitude);
        return new OutreachAtlas(list.Regions, (await areas).Areas, await overview, church);
    }

    private static MapFrame Box(MapBounds bounds) => MapAreaDrawing.Box(bounds);

    private static OutreachRegion? RegionAt(Placed[] regions, MapPoint point) =>
        Array.Find(regions, region => MapReach.Inside(region.Core, point))?.Region
        ?? Array.Find(regions, region => MapReach.Inside(region.Bounds, point))?.Region;

    private static bool Touch(MapFrame one, MapFrame other) =>
        one.South <= other.North && one.North >= other.South && one.West <= other.East && one.East >= other.West;

    private sealed record Placed(OutreachRegion Region, MapFrame Core, MapFrame Bounds)
    {
        public static Placed Of(OutreachRegion region) => new(region, Box(region.Core), Box(region.Bounds));
    }

    private static string Note(Maps.Contracts.MapArea area, AreaCell cell) => cell.Look switch
    {
        CellLook.Off => "Switched off",
        CellLook.Empty => "No homes",
        _ when area.State == MapAreaStates.Building => "Building…",
        _ => "Waiting to build",
    };

    private static MapFrame Padded(MapFrame frame) =>
        frame.IsEmpty ? frame : new MapFrame(frame.South - RoomToPan, frame.West - RoomToPan, frame.North + RoomToPan, frame.East + RoomToPan);
}
