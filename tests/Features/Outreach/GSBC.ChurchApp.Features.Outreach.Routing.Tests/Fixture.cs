using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Routing.Tests;

internal static class Fixture
{
    private static readonly Lazy<byte[]> Bytes = new(() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "albany-creek.streets")));

    public static readonly DateOnly Today = new(2026, 11, 14);

    public static GeoPoint Church => new(-27.34335, 152.96467);

    public static GeoPoint BesideEatonsCrossingRd => new(-27.32884, 152.95153);

    public static StreetGraph AlbanyCreek() => StreetGraphFile.Read(Bytes.Value);

    public static byte[] AlbanyCreekBytes => Bytes.Value;

    public static WalkPlanner Planner(IReadOnlyList<CampaignCoverage>? carried = null, OutreachMarks? marks = null, WalkKind walk = WalkKind.Letterbox, RouteSettings? settings = null) =>
        new(new Openness(new WalkNetwork(AlbanyCreek()), carried ?? [], Today, settings ?? RouteSettings.Defaults, walk, marks));

    public static int Start(this WalkPlanner planner, GeoPoint point) => planner.StartNear(point) ?? throw new InvalidOperationException("No street near the start.");

    public static IEnumerable<SideStretch> DeliveredSides(this WalkPlan plan, WalkNetwork network) =>
        plan.Arcs.Where(a => plan.Legs.Any(l => l.Delivers && l.Arcs.Contains(a)))
            .Select(a => new SideStretch(network[a.Piece].Street, a.Side, network[a.Piece].From, network[a.Piece].To));
}
