using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Ui.Maps;
using MapAreaShape = GSBC.ChurchApp.Platform.Ui.Maps.MapArea;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public sealed class OutreachAtlas
{
    private const string ChurchArea = "church";
    private const double RoomToPan = 0.05;
    private const double AroundChurch = 0.025;
    private static readonly IReadOnlySet<CellRef> NothingChanged = new HashSet<CellRef>();

    public OutreachAtlas(IReadOnlyList<OutreachRegion> regions, IReadOnlyList<Maps.Contracts.MapArea> areas)
    {
        Regions = [.. regions.Where(region => region.Map.Url.Length > 0).OrderBy(region => region.Id, StringComparer.Ordinal)];
        List<AreaCell> unbuilt = [.. areas.SelectMany(area => MapAreaDrawing.Cells(area, NothingChanged)
            .Where(cell => cell.Look is CellLook.Waiting or CellLook.Off or CellLook.Empty)
            .Select(cell => cell with { Name = Note(area, cell) }))];
        List<MapFrame> covered = [.. Regions.Select(region => Box(region.Core))];
        MapReach built = new(covered, [], []);
        List<MapMarker> notes = [.. unbuilt.Select(cell => built.FreeSpot(Box(cell.Core)) is { } spot ? new MapMarker(spot, MapMarkerKind.Note, cell.Name) : null).OfType<MapMarker>()];
        IReadOnlyList<MapAreaShape> others = MapAreaDrawing.ShapesOutside(unbuilt, built);
        Reach = new MapReach(covered, others, notes);
        Limits = new MapReach(covered, others, notes, Padded(MapFrame.Around(covered.Concat(unbuilt.Select(cell => Box(cell.Core))).SelectMany(MapReach.Ring))));
    }

    public IReadOnlyList<OutreachRegion> Regions { get; }

    public MapReach Reach { get; }

    public MapReach Limits { get; }

    public MapSource? Source => Regions.Count == 0 ? null : MapSource.Urls(Regions.Select(region => region.Map.Url), Regions[0].Attribution);

    public MapFrame Opening
    {
        get
        {
            List<OutreachRegion> church = [.. Regions.Where(region => region.AreaId == ChurchArea)];
            MapFrame all = Around(church.Count > 0 ? church : Regions);
            (double lat, double lon) = ((all.South + all.North) / 2, (all.West + all.East) / 2);
            return all.IsEmpty ? all : new MapFrame(lat - AroundChurch, lon - AroundChurch, lat + AroundChurch, lon + AroundChurch, 24, 15);
        }
    }

    public static MapFrame Around(IEnumerable<OutreachRegion> regions) =>
        MapFrame.Around(regions.SelectMany(region => MapReach.Ring(Box(region.Core))), padding: 24, maxZoom: 15);

    public OutreachRegion? RegionAt(MapPoint point) =>
        Regions.FirstOrDefault(region => MapReach.Inside(Box(region.Core), point))
        ?? Regions.FirstOrDefault(region => MapReach.Inside(Box(region.Bounds), point));

    public Func<MapPoint, bool> Owns(OutreachRegion region) => point => RegionAt(point)?.Id == region.Id;

    public IEnumerable<OutreachRegion> In(MapFrame view) =>
        Regions.Where(region => region.Core.South < view.North && region.Core.North > view.South && region.Core.West < view.East && region.Core.East > view.West);

    public static async Task<OutreachAtlas> LoadAsync(OutreachMapsClient maps, IAccess access)
    {
        Task<RegionList> regions = maps.RegionsAsync();
        Task<MapAreaList> areas = access.Can(OutreachMapsPermissions.Manage) ? maps.AreasAsync() : Task.FromResult(new MapAreaList());
        await Task.WhenAll(regions, areas);
        return new OutreachAtlas((await regions).Regions, (await areas).Areas);
    }

    private static MapFrame Box(MapBounds bounds) => MapAreaDrawing.Box(bounds);

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
