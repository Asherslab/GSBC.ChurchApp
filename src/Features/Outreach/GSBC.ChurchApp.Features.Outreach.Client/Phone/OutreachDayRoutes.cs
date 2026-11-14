using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed record RouteLine(DayRoute Route, IReadOnlyList<Claim> Claims, bool Mine);

public sealed record DayView(OutreachDay? Day, Campaign? Campaign, IReadOnlyList<RouteLine> Routes, bool On, bool Over);

public enum RouteState
{
    Ready,
    NotOnThisPhone,
    Over,
    GettingTheMap,
}

public sealed record DayRouteView(
    RouteState State,
    DayRoute? Route,
    OutreachDay? Day,
    Campaign? Campaign,
    IReadOnlyList<Claim> Claims,
    Claim? Mine,
    string FirstName,
    RegionOnPhone? Region,
    MapSource? Source,
    RouteToWalk? Walk,
    IReadOnlyList<MapLine> Lines,
    IReadOnlyList<MapMarker> Markers,
    MapFrame? Frame,
    WalkState? Walking);

public sealed class OutreachDayRoutes(OutreachStore store, OutreachClaims claims, RegionMaps regions, WalkChoices choices, WalkSession walk, TimeProvider time)
{
    public static readonly IReadOnlyList<string> Feeds = OutreachStore.Feeds;

    public async Task<DayView> DayAsync(string dayId)
    {
        OutreachNow now = await store.LoadAsync();
        OutreachDay? day = now.Days.FirstOrDefault(candidate => candidate.Id == dayId);
        if (day is null)
            return new DayView(null, null, [], false, false);
        IReadOnlyList<Claim> all = await claims.WithMineAsync(now);
        HashSet<string> mine = [.. (await claims.MineAsync()).Select(claim => claim.Id)];
        DateTime utc = time.GetUtcNow().UtcDateTime;
        return new DayView(day, now.CampaignOf(day.Campaign),
            [.. now.RoutesOf(day.Id).Select(route =>
            {
                List<Claim> on = [.. all.Where(claim => claim.RouteId == route.Id).OrderBy(claim => claim.At)];
                return new RouteLine(route, on, on.Any(claim => mine.Contains(claim.Id)));
            })],
            day.StartsAt <= utc && day.EndsAt > utc, day.EndsAt <= utc);
    }

    public async Task<DayRouteView> RouteAsync(string code)
    {
        OutreachNow now = await store.LoadAsync();
        WalkState? walking = await walk.LoadAsync();
        string firstName = await choices.FirstNameAsync();
        DayRoute? route = now.Routes.FirstOrDefault(candidate => string.Equals(candidate.Code, code, StringComparison.OrdinalIgnoreCase));
        OutreachDay? day = route is null ? null : now.Days.FirstOrDefault(candidate => candidate.Id == route.DayId);
        if (route is null || day is null)
            return new DayRouteView(RouteState.NotOnThisPhone, route, day, null, [], null, firstName, null, null, null, [], [], null, walking);
        Campaign? campaign = now.CampaignOf(day.Campaign);
        IReadOnlyList<Claim> all = await claims.WithMineAsync(now);
        HashSet<string> mineIds = [.. (await claims.MineAsync()).Select(claim => claim.Id)];
        List<Claim> on = [.. all.Where(claim => claim.RouteId == route.Id).OrderBy(claim => claim.At)];
        Claim? mine = on.FirstOrDefault(claim => mineIds.Contains(claim.Id));
        DayRouteView Bare(RouteState state, RegionOnPhone? region = null, MapSource? source = null) =>
            new(state, route, day, campaign, on, mine, firstName, region, source, null, [], [], null, walking);
        if (day.EndsAt <= time.GetUtcNow().UtcDateTime)
            return Bare(RouteState.Over);
        OutreachRegion? region = route.Stretches.Select(stretch => now.Regions.FirstOrDefault(candidate => candidate.Id == stretch.Region)).OfType<OutreachRegion>().FirstOrDefault()
                                 ?? RegionMaps.RegionAt(now.Regions, EngineInputs.Point(route.Start));
        if (region is null)
            return Bare(RouteState.NotOnThisPhone);
        RegionOnPhone onPhone = await regions.OnPhoneAsync(region);
        if (!onPhone.Kept)
        {
            await regions.KeepAsync(region, false);
            onPhone = await regions.OnPhoneAsync(region);
        }
        MapSource source = regions.SourceFor(onPhone);
        StreetGraph? graph = await regions.GraphAsync(region);
        if (graph is null)
            return Bare(RouteState.GettingTheMap, onPhone, source);
        RouteToWalk? planned = WalkPlanning.Route(now, region, graph, route, day, ChurchTime.Today(time));
        if (planned is null)
            return Bare(RouteState.NotOnThisPhone, onPhone, source);
        List<MapLine> lines = [.. WalkPlans.RouteLines(planned.Summary, null)];
        List<MapMarker> markers =
        [
            new(WalkPlans.Point(EngineInputs.Point(route.Start)), MapMarkerKind.Start, "Start"),
            .. day.MeetingPoint is { Lat: not 0 } meeting ? [new MapMarker(new MapPoint(meeting.Lat, meeting.Lng), MapMarkerKind.Meeting, day.MeetingPlace.Length > 0 ? day.MeetingPlace : "Meet here")] : Array.Empty<MapMarker>(),
            .. WalkPlans.Markers(planned.Summary),
        ];
        MapFrame frame = MapFrame.Around(planned.Summary.Line.Select(WalkPlans.Point), 36);
        return new DayRouteView(RouteState.Ready, route, day, campaign, on, mine, firstName, onPhone, source, planned, lines, markers, frame, walking);
    }
}

public sealed record MyWalk(string Id, DateTime StartedAt, int Metres, int Letterboxes, int Doors, int Conversations, WalkMode Mode, string Campaigns, bool Counted);

public sealed record MyOutreachView(IReadOnlyList<MyWalk> Walks, bool SignedIn, bool CountedByName, int Metres, int Letterboxes, int Conversations, int Doors);

public sealed class MyOutreach(OutreachStore store, WalkSession walk, Platform.Client.Access.IAccess access, Platform.Client.Outbox.IOutbox outbox)
{
    public static readonly IReadOnlyList<string> Feeds = [OutreachFeeds.Mine, OutreachFeeds.Campaigns];

    public async Task<MyOutreachView> LoadAsync()
    {
        OutreachNow now = await store.LoadAsync();
        IReadOnlyList<WalkHistoryEntry> kept = await walk.HistoryAsync();
        Dictionary<string, MyWalk> walks = [];
        foreach (WalkHistoryEntry entry in kept)
            walks[entry.WalkId] = new MyWalk(entry.WalkId, entry.StartedAt, entry.Metres, entry.Letterboxes, entry.Doors, entry.Conversations, entry.Mode,
                Names(now, entry.Campaigns), true);
        foreach (YourWalk yours in now.Mine)
            walks[yours.Id] = new MyWalk(yours.Id, yours.StartedAt, yours.Metres, yours.Letterboxes, yours.Doors, yours.Conversations, yours.Mode,
                Names(now, yours.Campaigns), yours.Counted);
        List<MyWalk> all = [.. walks.Values.OrderByDescending(entry => entry.StartedAt)];
        return new MyOutreachView(all, access.SignedIn, now.CountedByName, all.Sum(entry => entry.Metres), all.Sum(entry => entry.Letterboxes),
            all.Sum(entry => entry.Conversations), all.Sum(entry => entry.Doors));
    }

    public async Task CountByNameAsync(bool byName) =>
        await outbox.EnqueueAsync("/gsbc.outreach.Outreach/Counting", new CountingSave { ByName = byName }, OutreachContractsModel.Instance);

    private static string Names(OutreachNow now, IEnumerable<string> campaigns) =>
        string.Join(" and ", campaigns.Select(id => now.CampaignOf(id)?.Name).OfType<string>().DefaultIfEmpty(OutreachCampaigns.GeneralName));
}
