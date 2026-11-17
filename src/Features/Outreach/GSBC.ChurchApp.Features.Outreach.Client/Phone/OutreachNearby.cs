using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed record Nearby(OutreachNow Now, WalkState? Walk);

public sealed class OutreachNearby(OutreachStore store, WhereYouAre where, RegionMaps regions, WalkSession walk)
{
    public static readonly IReadOnlyList<string> Feeds = OutreachStore.Feeds;

    public async Task<Nearby> LoadAsync()
    {
        WalkState? walking = await walk.LoadAsync();
        OutreachNow now = await store.LoadAsync();
        if (await where.FindAsync(mayAsk: false) is { Point: { } here } && RegionMaps.RegionAt(now.Regions, here) is { } region
            && !(await regions.OnPhoneAsync(region)).Kept)
            await regions.KeepAsync(region, false);
        return new Nearby(now, walking);
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
