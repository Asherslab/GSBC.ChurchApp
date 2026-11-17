using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Ui.Maps;
using RoutingPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public enum PlanState
{
    Ready,
    NeedsLocation,
    NoMapsYet,
    OutsideTheArea,
    GettingTheMap,
    NoStreetNear,
}

public sealed record PlanRequest(WalkLength Length, RoutingPoint? Start, WalkMode Mode, bool TakeHop);

public sealed record PlanView(
    PlanState State,
    PlanRequest Request,
    OutreachNow Now,
    Here Here,
    IReadOnlyList<Campaign> Carried,
    RegionOnPhone? Region,
    MapSource? Source,
    PlannedWalk? Walk,
    IReadOnlyList<MapLine> Lines,
    IReadOnlyList<MapMarker> Markers,
    MapFrame? Frame,
    IReadOnlyList<string> StreetNames,
    StreetGraph? Graph = null)
{
    public MapYou? You => Request.Start is null && Here.Point is { } here ? new MapYou(new MapPoint(here.Lat, here.Lon), Here.Accuracy) : null;

    public RoutingPoint? StartPoint => Request.Start ?? Here.Point;
}

public sealed class WalkPlans(OutreachStore store, WhereYouAre where, RegionMaps regions, WalkChoices choices, TimeProvider time)
{
    public static readonly IReadOnlyList<string> Feeds = OutreachStore.Feeds;

    public async Task<PlanView> LoadAsync(PlanRequest request)
    {
        OutreachNow now = await store.LoadAsync();
        DateOnly today = ChurchTime.Today(time);
        IReadOnlyList<string> carrying = WalkPlanning.Carried(now, await choices.CarryingAsync(), today);
        IReadOnlyList<string> carried = request.Mode == WalkMode.Doorknock
            ? [WalkPlanning.KnockingFor(now, await choices.KnockingForAsync(), carrying, today)]
            : carrying;
        List<Campaign> campaigns = [.. carried.Select(now.CampaignOf).OfType<Campaign>()];
        Here here = request.Start is null ? await where.FindAsync(mayAsk: true) : new Here(HereState.Found, request.Start);
        PlanView Empty(PlanState state, RegionOnPhone? kept = null, MapSource? source = null) =>
            new(state, request, now, here, campaigns, kept, source, null, [], [], null, []);

        if (here.Point is not { } start)
            return Empty(PlanState.NeedsLocation);
        if (now.Regions.Count == 0)
            return Empty(PlanState.NoMapsYet);
        if (RegionMaps.RegionAt(now.Regions, start) is not { } region)
            return Empty(PlanState.OutsideTheArea);
        RegionOnPhone onPhone = await regions.OnPhoneAsync(region);
        if (!onPhone.Kept)
        {
            await regions.KeepAsync(region, false);
            onPhone = await regions.OnPhoneAsync(region);
        }
        StreetGraph? graph = await regions.GraphAsync(region);
        if (graph is null)
            return Empty(PlanState.GettingTheMap, onPhone, regions.SourceFor(onPhone));
        WalkAsk ask = new(request.Length.Budget, start, carried, request.Mode, request.TakeHop);
        PlannedWalk? walk = WalkPlanning.Plan(now, region, graph, ask, today);
        MapSource source = regions.SourceFor(onPhone);
        List<string> names = [.. Enumerable.Range(0, graph.StreetCount).Select(street => graph.Street(street).Name)
            .Where(name => name.Length > 0 && name != WalkLeg.Unnamed).Distinct().Order()];
        if (walk is null)
            return Empty(PlanState.NoStreetNear, onPhone, source) with { StreetNames = names, Graph = graph };
        List<MapLine> lines = [.. OutreachNearby.WalkedLines(now, graph, carried), .. RouteLines(walk.Summary, walk.Plan.Hop is not null ? walk.Reach.Hop : null)];
        List<MapMarker> markers = [.. request.Start is null ? [] : new[] { new MapMarker(Point(start), MapMarkerKind.Pin, "Start") }, .. Markers(walk.Summary)];
        List<MapPoint> framed = [.. walk.Summary.Line.Select(Point), Point(start)];
        return new PlanView(PlanState.Ready, request, now, here, campaigns, onPhone, source, walk, lines, markers, MapFrame.Around(framed, 36), names, graph);
    }

    public RoutingPoint? CentreOf(PlanView? view, string name)
    {
        if (view?.Graph is not { } graph)
            return null;
        int? street = Enumerable.Range(0, graph.StreetCount).Where(index => graph.Street(index).Name == name)
            .OrderByDescending(index => graph.Homes(index)).Cast<int?>().FirstOrDefault();
        return street is { } found ? graph.Projection.ToGeo(graph.PointAt(found, 0.5)) : null;
    }

    public static IEnumerable<MapLine> RouteLines(RouteSummary summary, HopChoice? hop) =>
        summary.Steps.Select(step => new MapLine([.. step.Line.Select(Point)], step.Delivers ? MapLineStyle.Walk
            : hop is not null && step.Metres >= hop.Hop.Metres * 0.9 ? MapLineStyle.LongWalk : MapLineStyle.Connector,
            !step.Delivers && hop is not null && step.Metres >= hop.Hop.Metres * 0.9 ? $"{OutreachPhoneWords.Distance(step.Metres)}, no letterboxes" : null));

    public static IEnumerable<MapMarker> Markers(RouteSummary summary) =>
        summary.Steps.SelectMany(step => step.Markers).Select(marker => new MapMarker(Point(marker.Point), marker.Kind switch
        {
            MarkerKind.NoJunkMail => MapMarkerKind.NoJunkMail,
            MarkerKind.DontLetterbox => MapMarkerKind.DontLetterbox,
            MarkerKind.Check => MapMarkerKind.Check,
            _ => MapMarkerKind.Skip,
        }, marker.Kind is MarkerKind.NoJunkMail or MarkerKind.DontLetterbox ? marker.Label : null));

    public static MapPoint Point(RoutingPoint point) => new(point.Lat, point.Lon);
}
