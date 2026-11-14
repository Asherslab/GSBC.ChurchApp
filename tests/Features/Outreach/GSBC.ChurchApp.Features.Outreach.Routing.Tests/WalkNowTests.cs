using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Routing.Tests;

public sealed class WalkNowTests
{
    private static readonly WalkBudget AnHour = new(Minutes: 60);

    [Fact]
    public void AnHourFromChurchIsAClosedLoopOverBothSidesOfEveryStreetItDelivers()
    {
        WalkPlanner planner = Fixture.Planner();

        WalkPlan plan = planner.Plan(planner.Start(Fixture.Church), AnHour);

        Assert.InRange(plan.Time.TotalMinutes, 54, 62);
        Assert.InRange(plan.Metres, 2500, 3500);
        Assert.InRange(plan.Letterboxes, 80, 140);
        for (int i = 0; i < plan.Arcs.Count; i++)
            Assert.Equal(End(planner, plan.Arcs[i]), Begin(planner, plan.Arcs[(i + 1) % plan.Arcs.Count]));
        Assert.Equal(plan.Start, Begin(planner, plan.Arcs[0]));
        foreach (int piece in plan.Pieces)
        {
            Assert.Contains(new Arc(piece, true), plan.Arcs);
            Assert.Contains(new Arc(piece, false), plan.Arcs);
        }
    }

    [Fact]
    public void BundlesAreTheLetterboxesInFiftiesWithASpareWhenTheRemainderIsClose()
    {
        WalkPlanner planner = Fixture.Planner();

        WalkPlan plan = planner.Plan(planner.Start(Fixture.Church), AnHour);

        Assert.Equal((plan.Letterboxes + 49) / 50, plan.Bundles);
        Assert.Equal((int)Math.Ceiling(plan.Letterboxes * 1.1 / 50), plan.BundlesToTake);
    }

    [Fact]
    public void MainRoadsAreWalkedButNeverDelivered()
    {
        WalkPlanner planner = Fixture.Planner();
        foreach (GeoPoint start in Starts())
        {
            WalkPlan plan = planner.Plan(planner.Start(start), AnHour);
            Assert.DoesNotContain(plan.Deliveries.SelectMany(l => l.Arcs), arc => planner.Network.Street(arc.Piece).Kind.IsMainRoad());
        }
    }

    [Fact]
    public void MainRoadsStayInTheNetworkSoAlmostEveryHomeIsReachable()
    {
        StreetGraph graph = Fixture.AlbanyCreek();
        int all = Enumerable.Range(0, graph.StreetCount).Sum(graph.Homes);

        double withTrunkRoads = LargestPiece(graph, _ => true) / (double)all;
        double withoutTrunkRoads = LargestPiece(graph, s => graph.Street(s).Kind != RoadKind.Trunk) / (double)all;

        Assert.True(withTrunkRoads >= 0.985, $"{withTrunkRoads:P1} of homes in the largest piece");
        Assert.True(withoutTrunkRoads < 0.9, $"{withoutTrunkRoads:P1} of homes in the largest piece without trunk roads");
    }

    [Fact]
    public void TheWalkIsTakenOneSideOfOneStreetAtATime()
    {
        WalkPlanner planner = Fixture.Planner();

        WalkPlan plan = planner.Plan(planner.Start(Fixture.Church), AnHour);

        foreach (WalkLeg leg in plan.Legs)
        {
            Assert.All(leg.Arcs, arc => Assert.Equal(leg.Street, planner.Network.Street(arc.Piece).Name is { Length: > 0 } name ? name : WalkLeg.Unnamed));
            for (int i = 1; i < leg.Arcs.Count; i++)
                Assert.False(planner.Network[leg.Arcs[i].Piece].Street == planner.Network[leg.Arcs[i - 1].Piece].Street && leg.Arcs[i].Forward != leg.Arcs[i - 1].Forward);
        }
        Assert.Equal(plan.Letterboxes, plan.Legs.Sum(l => l.Letterboxes));
    }

    [Fact]
    public void ADoorknockWalkVisitsFarFewerDoorsInTheSameTime()
    {
        WalkPlanner letterboxes = Fixture.Planner();
        WalkPlanner doors = Fixture.Planner(walk: WalkKind.Doorknock);

        WalkPlan drop = letterboxes.Plan(letterboxes.Start(Fixture.Church), AnHour);
        WalkPlan knock = doors.Plan(doors.Start(Fixture.Church), AnHour);

        Assert.InRange(knock.Time.TotalMinutes, 50, 62);
        Assert.True(knock.Letterboxes * 3 < drop.Letterboxes, $"{knock.Letterboxes} doors against {drop.Letterboxes} letterboxes");
    }

    [Fact]
    public void AWalkByDistanceComesToAboutThatDistance()
    {
        WalkPlanner planner = Fixture.Planner();

        WalkPlan plan = planner.Plan(planner.Start(Fixture.Church), new WalkBudget(Kilometres: 4));

        Assert.InRange(plan.Metres, 3600, 4100);
    }

    internal static IEnumerable<GeoPoint> Starts()
    {
        Random random = new(20261114);
        GeoBounds box = Fixture.AlbanyCreek().Region.Bounds;
        for (int i = 0; i < 12; i++)
            yield return new GeoPoint(box.South + random.NextDouble() * (box.North - box.South), box.West + random.NextDouble() * (box.East - box.West));
    }

    private static int LargestPiece(StreetGraph graph, Func<int, bool> kept)
    {
        int[] group = Enumerable.Range(0, graph.NodeCount).ToArray();
        int Find(int n) => group[n] == n ? n : group[n] = Find(group[n]);
        List<int> streets = Enumerable.Range(0, graph.StreetCount).Where(kept).ToList();
        foreach (int s in streets)
            group[Find(graph.Street(s).U)] = Find(graph.Street(s).V);
        return streets.GroupBy(s => Find(graph.Street(s).U)).Max(g => g.Sum(graph.Homes));
    }

    private static int Begin(WalkPlanner planner, Arc arc) => arc.Forward ? planner.Network[arc.Piece].U : planner.Network[arc.Piece].V;

    private static int End(WalkPlanner planner, Arc arc) => arc.Forward ? planner.Network[arc.Piece].V : planner.Network[arc.Piece].U;
}
