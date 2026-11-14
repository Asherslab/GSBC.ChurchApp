namespace GSBC.ChurchApp.Features.Outreach.Routing.Tests;

public sealed class FollowingTests
{
    [Theory]
    [InlineData(1, 4, 2, 0.95, 0.05)]
    [InlineData(5, 6, 3, 0.93, 0.07)]
    [InlineData(45, 10, 4, 0.85, 0.12)]
    public void WalkingThePlannedRouteMarksItsLetterboxesAndHardlyAnyOthers(double everySeconds, double noise, double drift, double recall, double extra)
    {
        WalkPlanner planner = Fixture.Planner();
        WalkPlan plan = planner.Plan(planner.Start(Fixture.Church), new WalkBudget(Minutes: 60));
        WalkFollower follower = Follow(planner.Network, plan.Arcs, new TrackStyle(everySeconds, noise, drift));

        HashSet<int> planned = Tracks.Homes(planner.Network.Graph, RouteSummary.Of(plan, planner.Open).Delivered);
        HashSet<int> marked = Tracks.Homes(planner.Network.Graph, follower.Covered);

        double found = planned.Count(marked.Contains) / (double)planned.Count;
        double stray = marked.Count(b => !planned.Contains(b)) / (double)Math.Max(1, marked.Count);
        TestContext.Current.TestOutputHelper?.WriteLine($"every {everySeconds}s: {found:P1} found, {stray:P1} stray, {planned.Count} planned, {marked.Count} marked, {follower.Fixes.Count} fixes");
        Assert.True(found >= recall, $"{found:P0} of the route's letterboxes marked");
        Assert.True(stray <= extra, $"{stray:P0} of marked letterboxes weren't on the route");
    }

    [Fact]
    public void CrossingAStreetDoesNotMarkIt()
    {
        StreetGraph graph = Fixture.AlbanyCreek();
        WalkNetwork network = new(graph);
        (int along, int across) = Crossroads(graph);
        Flat centre = graph.NodeFlat(graph.Street(along).V);
        Flat direction = graph.NodeFlat(graph.Street(along).V) - graph.NodeFlat(graph.Street(along).U);
        direction *= 1 / direction.Length;
        Flat kerb = new Flat(-direction.Y, direction.X) * 7;
        WalkFollower follower = new(graph);
        foreach (GpsFix fix in Tracks.Straight(graph, centre - direction * 60 + kerb, centre + direction * 60 + kerb, TrackStyle.InHand))
            follower.Add(fix);

        TestContext.Current.TestOutputHelper?.WriteLine(string.Join("; ", follower.Covered.Select(c => $"{graph.Street(c.Street).Name}#{c.Street} {c.Side} {c.From:0.00}-{c.To:0.00}")) + $" along {along} across {across}");
        Assert.Contains(follower.Covered, s => s.Street == along || graph.Street(s.Street).Name == graph.Street(along).Name);
        Assert.DoesNotContain(follower.Covered, s => s.Street == across);
        Assert.NotNull(network);
    }

    [Fact]
    public void WhatWasActuallyWalkedCountsNotWhatWasPlanned()
    {
        WalkPlanner planner = Fixture.Planner();
        WalkPlan plan = planner.Plan(planner.Start(Fixture.Church), new WalkBudget(Minutes: 60));
        WalkLeg skippedLeg = plan.Deliveries.Where(l => l.Letterboxes >= 5).OrderBy(l => l.Arcs.Count).First();
        WalkPlan other = planner.Plan(planner.Start(new GeoPoint(-27.3625, 152.9990)), new WalkBudget(Minutes: 20));
        List<Arc> walked = [.. plan.Arcs.Where(a => !skippedLeg.Arcs.Contains(a))];

        WalkFollower follower = new(planner.Network.Graph);
        foreach (GpsFix fix in Tracks.Walk(planner.Network, other.Arcs, TrackStyle.InHand))
            follower.Add(fix);

        HashSet<int> marked = Tracks.Homes(planner.Network.Graph, follower.Covered);
        HashSet<int> otherHomes = Tracks.Homes(planner.Network.Graph, RouteSummary.Of(other, planner.Open).Delivered);
        HashSet<int> planned = Tracks.Homes(planner.Network.Graph, RouteSummary.Of(plan, planner.Open).Delivered);
        Assert.True(otherHomes.Count(marked.Contains) >= otherHomes.Count * 0.9);
        Assert.True(planned.Except(otherHomes).Count(marked.Contains) <= planned.Count * 0.05);
        Assert.NotEmpty(walked);
    }

    [Fact]
    public void UpOneSideOfACulDeSacAndBackDownTheOtherMarksBothSides()
    {
        WalkPlanner planner = Fixture.Planner();
        StreetGraph graph = planner.Network.Graph;
        int culDeSac = Enumerable.Range(0, graph.StreetCount)
            .Where(s => graph.Street(s).CanDeliver && graph.Street(s).Length > 120 && graph.Homes(s, Side.Left) >= 4 && graph.Homes(s, Side.Right) >= 4)
            .First(s => graph.StreetsAt(graph.Street(s).V).Length == 1);

        WalkFollower follower = Follow(planner.Network, [new Arc(culDeSac, true), new Arc(culDeSac, false)], TrackStyle.InHand);

        Assert.Contains(follower.Covered, s => s.Street == culDeSac && s.Side == Side.Left && s.To - s.From > 0.8);
        Assert.Contains(follower.Covered, s => s.Street == culDeSac && s.Side == Side.Right && s.To - s.From > 0.8);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(4, 2)]
    public void AWalkerOnTheOtherKerbMarksTheSideTheyWereActuallyOn(double noise, double drift)
    {
        WalkPlanner planner = Fixture.Planner();
        StreetGraph graph = planner.Network.Graph;
        List<int> streets = Enumerable.Range(0, graph.StreetCount)
            .Where(s => graph.Street(s).CanDeliver && graph.Street(s).Length > 150 && graph.Homes(s, Side.Right) >= 5)
            .Take(10)
            .ToList();
        int right = 0;
        foreach (int street in streets)
        {
            WalkFollower follower = new(graph);
            foreach (GpsFix fix in Tracks.Walk(planner.Network, [new Arc(street, true)], new TrackStyle(1, noise, drift, KerbMetres: -8, Seed: street)))
                follower.Add(fix);
            right += follower.Covered.Any(c => c.Street == street && c.Side == Side.Right) && !follower.Covered.Any(c => c.Street == street && c.Side == Side.Left) ? 1 : 0;
        }

        Assert.True(right >= streets.Count * 0.8, $"{right} of {streets.Count} walks marked the side walked");
    }

    [Fact]
    public void ABitTheWalkerSkippedStaysOpen()
    {
        WalkPlanner planner = Fixture.Planner();
        WalkPlan plan = planner.Plan(planner.Start(Fixture.Church), new WalkBudget(Minutes: 30));
        WalkFollower follower = Follow(planner.Network, plan.Arcs, TrackStyle.InHand);
        SideStretch skipped = follower.Covered.First(s => s.To - s.From > 0.5);

        follower.Skip(skipped with { From = skipped.From, To = (skipped.From + skipped.To) / 2 });

        Assert.DoesNotContain(follower.Covered, s => s.Street == skipped.Street && s.Side == skipped.Side && s.From < (skipped.From + skipped.To) / 2 - 1e-9 && s.To > skipped.From + 1e-9);
    }

    [Fact]
    public void FixesTooRoughToTrustAreLeftOut()
    {
        WalkPlanner planner = Fixture.Planner();
        WalkPlan plan = planner.Plan(planner.Start(Fixture.Church), new WalkBudget(Minutes: 30));
        WalkFollower follower = new(planner.Network.Graph);

        foreach (GpsFix fix in Tracks.Walk(planner.Network, plan.Arcs, TrackStyle.InHand))
            follower.Add(fix with { Accuracy = 80 });

        Assert.Empty(follower.Covered);
    }

    [Fact]
    public void TheShareOfARouteDoneFollowsTheWalk()
    {
        WalkPlanner planner = Fixture.Planner();
        WalkPlan plan = planner.Plan(planner.Start(Fixture.Church), new WalkBudget(Minutes: 60));
        RouteSummary summary = RouteSummary.Of(plan, planner.Open);
        WalkFollower follower = new(planner.Network.Graph);
        List<GpsFix> track = Tracks.Walk(planner.Network, plan.Arcs, TrackStyle.InHand);

        foreach (GpsFix fix in track.Take(track.Count / 2))
            follower.Add(fix);
        double half = follower.ShareDone(summary.Delivered);
        foreach (GpsFix fix in track.Skip(track.Count / 2))
            follower.Add(fix);

        Assert.InRange(half, 0.2, 0.8);
        Assert.True(follower.ShareDone(summary.Delivered) >= 0.95);
    }

    private static WalkFollower Follow(WalkNetwork network, IEnumerable<Arc> arcs, TrackStyle style)
    {
        WalkFollower follower = new(network.Graph);
        foreach (GpsFix fix in Tracks.Walk(network, arcs, style))
            follower.Add(fix);
        return follower;
    }

    private static (int Along, int Across) Crossroads(StreetGraph graph)
    {
        for (int node = 0; node < graph.NodeCount; node++)
        {
            ReadOnlySpan<int> streets = graph.StreetsAt(node);
            if (streets.Length != 4)
                continue;
            foreach (int a in streets)
            {
                foreach (int b in streets)
                {
                    if (a == b || graph.Street(a).Name != graph.Street(b).Name || graph.Street(a).Length < 70 || graph.Street(b).Length < 70 || !graph.Street(a).CanDeliver)
                        continue;
                    int across = -1;
                    foreach (int c in streets)
                    {
                        if (c != a && c != b && graph.Street(c).CanDeliver && graph.Street(c).Length > 40)
                            across = c;
                    }
                    if (across >= 0 && graph.Street(a).V == node && graph.Street(b).U == node && graph.Street(a).Kind != RoadKind.Trunk)
                        return (a, across);
                }
            }
        }
        throw new InvalidOperationException("No crossroads in the fixture.");
    }
}
