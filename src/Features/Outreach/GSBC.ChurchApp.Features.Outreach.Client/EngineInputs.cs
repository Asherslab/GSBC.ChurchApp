using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Common;
using RoutingPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;
using RecordPoint = GSBC.ChurchApp.Features.Outreach.Contracts.GeoPoint;
using RecordStretch = GSBC.ChurchApp.Features.Outreach.Contracts.Stretch;
using RoutingCovered = GSBC.ChurchApp.Features.Outreach.Routing.CoveredStretch;
using RecordCovered = GSBC.ChurchApp.Features.Outreach.Contracts.CoveredStretch;

namespace GSBC.ChurchApp.Features.Outreach.Client;

public static class EngineInputs
{
    public static RouteSettings Settings(OutreachSettings settings, Campaign? campaign) => Settings(settings) with { BundleSize = CampaignStages.BundleSizeOf(campaign, settings) };

    public static RouteSettings Settings(OutreachSettings settings) => new()
    {
        WalkingKmh = settings.WalkingKmh,
        HouseSeconds = settings.HouseSeconds,
        UnitSeconds = settings.UnitSeconds,
        DoorSeconds = settings.DoorSeconds,
        UnitDoorSeconds = settings.UnitDoorSeconds,
        BundleSize = settings.BundleSize,
        SpareShare = settings.SpareShare,
        RestDays = settings.RestDays,
        MaxConnectorLegMetres = settings.MaxConnectorLegMetres,
        MaxConnectorTotalMetres = settings.MaxConnectorTotalMetres,
        SplitJoinMetres = settings.SplitJoinMetres,
        MaxHopMetres = settings.MaxHopMetres,
        MaxApproachMetres = settings.MaxApproachMetres,
        ShortOf = settings.ShortOf,
        LongOf = settings.LongOf,
    };

    public static RoutingPoint Point(RecordPoint point) => new(point.Lat, point.Lng);

    public static RecordPoint Point(RoutingPoint point) => new() { Lat = point.Lat, Lng = point.Lon };

    public static Side Side(StreetSide side) => side == StreetSide.Right ? Routing.Side.Right : Routing.Side.Left;

    public static StreetSide Side(Side side) => side == Routing.Side.Right ? StreetSide.Right : StreetSide.Left;

    public static SideStretch? SideStretch(StreetGraph graph, RecordStretch stretch) =>
        StreetKey.TryParse(stretch.Street, out StreetKey key) ? SideStretch(graph, key, stretch.Side, stretch.From, stretch.To) : null;

    public static SideStretch? SideStretch(StreetGraph graph, StreetKey key, RecordCovered covered) =>
        SideStretch(graph, key, covered.Side, covered.From, covered.To);

    private static SideStretch? SideStretch(StreetGraph graph, StreetKey key, StreetSide side, int from, int to) =>
        graph.StreetOf(key) is { } street && from < to
            ? new SideStretch(street, Side(side), from / (double)OutreachRules.WholeStreet, to / (double)OutreachRules.WholeStreet)
            : null;

    public static RecordStretch Stretch(StreetGraph graph, string region, long version, SideStretch stretch)
    {
        StreetInfo street = graph.Street(stretch.Street);
        LetterboxRange boxes = graph.Between(stretch.Street, stretch.Side, stretch.From, stretch.To);
        int homes = 0;
        for (int b = boxes.Start; b < boxes.End; b++)
            if (graph.LetterboxKindOf(b).IsHome())
                homes++;
        return new RecordStretch
        {
            Region = region,
            RegionVersion = version,
            Street = graph.KeyOf(stretch.Street).ToString(),
            Side = Side(stretch.Side),
            From = (int)Math.Round(stretch.From * OutreachRules.WholeStreet),
            To = (int)Math.Round(stretch.To * OutreachRules.WholeStreet),
            Middle = Point(graph.Projection.ToGeo(graph.PointAt(stretch.Street, (stretch.From + stretch.To) / 2))),
            Metres = (int)Math.Round((stretch.To - stretch.From) * street.Length),
            Letterboxes = homes,
        };
    }

    public static AddressPoint Address(StreetGraph graph, string region, int letterbox)
    {
        Letterbox box = graph.LetterboxAt(letterbox);
        return new AddressPoint
        {
            Region = region,
            Street = graph.KeyOf(box.Street).ToString(),
            Side = Side(box.Side),
            Along = (int)Math.Round(box.Along * OutreachRules.WholeStreet),
            Point = Point(graph.Projection.ToGeo(graph.LetterboxFlat(letterbox))),
            Label = box.Label ?? "",
            AddressKey = box.AddressKey,
        };
    }

    public static CampaignCoverage Coverage(StreetGraph graph, string campaign, IEnumerable<StreetCoverage> streets) =>
        new(campaign, [.. streets
            .Where(street => street.Campaign == campaign)
            .SelectMany(street => street.Stretches.Select(covered => (street.Street, Covered: covered)))
            .Select(pair => SideStretch(graph, new RecordStretch { Street = pair.Street, Side = pair.Covered.Side, From = pair.Covered.From, To = pair.Covered.To }) is { } side
                ? new RoutingCovered(side, ChurchTime.DayOf(pair.Covered.At))
                : (RoutingCovered?)null)
            .OfType<RoutingCovered>()]);

    public static OutreachMarks Marks(IEnumerable<Skip> skips, IEnumerable<Exclusion> exclusions)
    {
        List<Skip> kept = [.. skips.Where(skip => skip.Address.AddressKey != 0)];
        List<Exclusion> excluded = [.. exclusions];
        return new OutreachMarks
        {
            NoJunkMail = kept.Where(skip => skip.Letterbox && !skip.Door).Select(skip => skip.Address.AddressKey).ToHashSet(),
            Hostile = kept.Where(skip => skip.Door).Select(skip => skip.Address.AddressKey).ToHashSet(),
            Excluded = excluded.Where(exclusion => exclusion.Address is { AddressKey: not 0 }).Select(exclusion => exclusion.Address!.AddressKey).ToHashSet(),
            ShownToWalkers = excluded.Where(exclusion => exclusion is { ShowMarker: true, Address.AddressKey: not 0 }).Select(exclusion => exclusion.Address!.AddressKey).ToHashSet(),
            ExcludedZones = [.. excluded.Where(exclusion => exclusion.Area.Count >= 3).Select(exclusion => (IReadOnlyList<RoutingPoint>)exclusion.Area.Select(Point).ToList())],
        };
    }
}
