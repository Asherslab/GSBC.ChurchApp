using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Routing.Tests;

public sealed class ReachTests
{
    private static readonly WalkBudget TwoHours = new(Minutes: 120);

    [Fact]
    public void AWalkInACutOffPocketSaysItIsBoxedInByTheMainRoad()
    {
        WalkPlanner planner = Fixture.Planner();
        int start = planner.Start(Fixture.BesideEatonsCrossingRd);
        WalkPlan plan = planner.Plan(start, TwoHours);

        WalkReach reach = new ReachFinder(planner).Assess(start, TwoHours, plan);

        Assert.Equal(ReachStatus.Short, reach.Status);
        Assert.True(reach.Capped);
        ReachReason boxedIn = Assert.Single(reach.Reasons, r => r.Kind == ReachReasonKind.BoxedIn);
        Assert.Contains("Eatons Crossing Rd", boxedIn.Names);
    }

    [Fact]
    public void ACappedWalkOffersALongerWalkBetweenStreetsThatIsOverTheLimitButWithinSixHundredMetres()
    {
        WalkPlanner planner = Fixture.Planner();
        int start = planner.Start(Fixture.BesideEatonsCrossingRd);
        WalkPlan plan = planner.Plan(start, TwoHours);

        HopChoice hop = new ReachFinder(planner).Assess(start, TwoHours, plan).Hop!;

        Assert.InRange(hop.Hop.Metres, RouteSettings.Defaults.MaxConnectorLegMetres, RouteSettings.Defaults.MaxHopMetres);
        Assert.Equal(hop.Hop, hop.Plan.Hop);
        Assert.True(hop.Added >= 20);
        Assert.Equal(plan.Letterboxes + hop.Added, hop.Plan.Letterboxes);
    }

    [Fact]
    public void ACappedWalkOffersTheNearestStartWithRoomForTheWholeWalk()
    {
        WalkPlanner planner = Fixture.Planner();
        int start = planner.Start(Fixture.BesideEatonsCrossingRd);
        WalkPlan plan = planner.Plan(start, TwoHours);

        StartChoice elsewhere = new ReachFinder(planner).Assess(start, TwoHours, plan).Elsewhere!;

        Assert.InRange(elsewhere.Metres, 1, 4000);
        Assert.True(elsewhere.Plan.Time.TotalMinutes >= 120 * RouteSettings.Defaults.ShortOf);
        Assert.False(string.IsNullOrEmpty(elsewhere.Street));
    }

    [Fact]
    public void WhenEverythingNearbyIsDoneTheWalkSaysSoAndSuggestsWhereToStart()
    {
        WalkPlanner fresh = Fixture.Planner();
        int start = fresh.Start(Fixture.Church);
        StreetGraph graph = fresh.Network.Graph;
        Flat church = graph.Projection.ToFlat(Fixture.Church);
        List<CoveredStretch> done = graph.Index.StreetsNear(church, 1200)
            .Where(s => Flat.Distance(graph.PointAt(s, 0.5), church) < 1200)
            .SelectMany(s => new[] { SideStretch.Whole(s, Side.Left), SideStretch.Whole(s, Side.Right) })
            .Select(s => new CoveredStretch(s, Fixture.Today.AddDays(-3)))
            .ToList();
        WalkPlanner planner = Fixture.Planner([new CampaignCoverage("General", done)]);
        WalkBudget anHour = new(Minutes: 60);
        WalkPlan plan = planner.Plan(start, anHour);

        WalkReach reach = new ReachFinder(planner).Assess(start, anHour, plan);

        Assert.NotEqual(ReachStatus.Full, reach.Status);
        Assert.NotNull(reach.Elsewhere);
        Assert.InRange(reach.Elsewhere.Metres, 500, 4000);
        Assert.Equal(ReachStatus.Full, new ReachFinder(planner).StatusOf(anHour, reach.Elsewhere.Plan));
    }

    [Fact]
    public void AWalkThatComesOutAboutAsAskedNeedsNoNote()
    {
        WalkPlanner planner = Fixture.Planner();
        int start = planner.Start(Fixture.Church);

        WalkReach reach = new ReachFinder(planner).Assess(start, TwoHours, planner.Plan(start, TwoHours));

        Assert.Same(WalkReach.Full, reach);
    }
}
