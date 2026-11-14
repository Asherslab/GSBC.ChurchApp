using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Client.Location;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed record WalkView(
    WalkState? State,
    WalkProgress? Progress,
    MapSource? Source,
    IReadOnlyList<MapLine> Lines,
    IReadOnlyList<MapMarker> Markers,
    MapFrame? Frame,
    MapYou? You,
    string Carrying,
    bool Tracking,
    int BundleSize,
    int Waiting = 0,
    int Failed = 0);

public sealed class WalkScreen(WalkSession walk, OutreachStore store, RegionMaps regions, ILocationSession location, IOutbox outbox)
{
    public static readonly IReadOnlyList<string> Feeds = [OutreachFeeds.Settings, OutreachFeeds.Campaigns, OutreachMapsFeeds.Regions];

    public async Task<WalkView> LoadAsync()
    {
        WalkState? state = await walk.LoadAsync();
        if (state is null)
            return new WalkView(null, null, null, [], [], null, null, "", false, 50);
        OutreachNow now = await store.LoadAsync();
        OutreachRegion? region = now.Regions.FirstOrDefault(candidate => candidate.Id == state.RegionId);
        MapSource? source = region is null ? null : regions.SourceFor(await regions.OnPhoneAsync(region));
        WalkProgress? progress = walk.Progress;
        List<MapLine> lines = [.. walk.Graph is { } graph ? OutreachNearby.WalkedLines(now, graph, state.Campaigns) : [], .. state.Steps.Select((step, index) => new MapLine(
            [.. step.Line.Select(point => new MapPoint(point[0], point[1]))],
            !step.Delivers ? MapLineStyle.Connector
            : progress is not null && index < progress.Steps.Count && progress.Steps[index].Share >= WalkSession.StepDoneShare ? MapLineStyle.WalkDone
            : MapLineStyle.Walk))];
        List<MapMarker> markers = [.. state.Steps.SelectMany(step => step.Markers).Select(marker => new MapMarker(new MapPoint(marker.Lat, marker.Lon),
            (MarkerKind)marker.Kind switch
            {
                MarkerKind.NoJunkMail => MapMarkerKind.NoJunkMail,
                MarkerKind.DontLetterbox => MapMarkerKind.DontLetterbox,
                MarkerKind.Check => MapMarkerKind.Check,
                _ => MapMarkerKind.Skip,
            }, (MarkerKind)marker.Kind is MarkerKind.NoJunkMail or MarkerKind.DontLetterbox ? marker.Label : null))];
        MapFrame frame = MapFrame.Around(state.Steps.SelectMany(step => step.Line).Select(point => new MapPoint(point[0], point[1])), 32);
        MapYou? you = progress?.You is { } at ? new MapYou(new MapPoint(at.Lat, at.Lon), progress.Accuracy) : null;
        List<Campaign> carried = [.. state.Campaigns.Select(now.CampaignOf).OfType<Campaign>()];
        bool tracking = state.Tracking && (await location.StateAsync()).Running || state.Paused;
        List<OutboxEntry> queued = [.. (await outbox.ListAsync()).Where(entry => entry.Method.StartsWith(OutreachMethods.Under, StringComparison.Ordinal))];
        return new WalkView(state, progress, source, lines, markers, frame, you, carried.Count == 0 ? "General outreach" : string.Join(" + ", carried.Select(c => c.Name)),
            tracking, now.Settings.BundleSize, queued.Count(entry => !entry.Failed), queued.Count(entry => entry.Failed));
    }
}
