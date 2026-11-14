using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Ui.Maps;
using RoutingPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed record Nearby(
    OutreachNow Now,
    Here Here,
    RegionOnPhone? Region,
    MapSource? Source,
    IReadOnlyList<MapLine> Walked,
    IReadOnlyList<Campaign> Carried,
    MapPoint? Centre,
    WalkState? Walk);

public sealed class OutreachNearby(OutreachStore store, WhereYouAre where, RegionMaps regions, WalkChoices choices, WalkSession walk, TimeProvider time)
{
    public static readonly IReadOnlyList<string> Feeds = OutreachStore.Feeds;

    public async Task<Nearby> LoadAsync()
    {
        WalkState? walking = await walk.LoadAsync();
        OutreachNow now = await store.LoadAsync();
        Here here = await where.FindAsync(mayAsk: false);
        IReadOnlyList<string> carried = WalkPlanning.Carried(now, await choices.CarryingAsync(), Platform.Common.ChurchTime.Today(time));
        List<Campaign> campaigns = [.. carried.Select(now.CampaignOf).OfType<Campaign>()];
        OutreachRegion? region = here.Point is { } point ? RegionMaps.Nearest(now.Regions, point) : now.Regions.FirstOrDefault();
        if (region is null)
            return new Nearby(now, here, null, null, [], campaigns, null, walking);
        RegionOnPhone onPhone = await regions.OnPhoneAsync(region);
        if (!onPhone.Kept)
            await regions.KeepAsync(region, false);
        StreetGraph? graph = await regions.GraphAsync(region);
        RoutingPoint centre = here.Point ?? RegionMaps.Bounds(region.Core).Centre;
        return new Nearby(now, here, onPhone, regions.SourceFor(onPhone), graph is null ? [] : WalkedLines(now, graph, carried), campaigns,
            new MapPoint(centre.Lat, centre.Lon), walking);
    }

    public static IReadOnlyList<MapLine> WalkedLines(OutreachNow now, StreetGraph graph, IReadOnlyList<string> campaigns)
    {
        HashSet<string> carried = [.. campaigns];
        List<MapLine> lines = [];
        foreach (StreetCoverage street in now.Coverage.Where(street => carried.Contains(street.Campaign)))
        {
            if (!StreetKey.TryParse(street.Street, out StreetKey key) || graph.StreetOf(key) is not { } index)
                continue;
            foreach (Contracts.CoveredStretch stretch in street.Stretches)
                lines.Add(new MapLine([.. graph.ShapePoints(index, stretch.From / (double)OutreachRules.WholeStreet, stretch.To / (double)OutreachRules.WholeStreet)
                    .Select(point => new MapPoint(point.Lat, point.Lon))], MapLineStyle.Walked));
        }
        return lines;
    }
}
