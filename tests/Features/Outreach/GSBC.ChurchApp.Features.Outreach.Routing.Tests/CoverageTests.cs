namespace GSBC.ChurchApp.Features.Outreach.Routing.Tests;

public sealed class CoverageTests
{
    private static readonly WalkBudget AnHour = new(Minutes: 60);

    [Fact]
    public void AWalkAvoidsStreetsTheCampaignCoveredWithinTheRestPeriod()
    {
        WalkPlanner fresh = Fixture.Planner();
        WalkPlan first = fresh.Plan(fresh.Start(Fixture.Church), AnHour);
        CampaignCoverage general = Covered("General", first, fresh.Network, Fixture.Today.AddDays(-20));

        WalkPlanner planner = Fixture.Planner([general]);
        WalkPlan second = planner.Plan(planner.Start(Fixture.Church), AnHour);

        Assert.True(second.Letterboxes > 60);
        Assert.Empty(first.DeliveredSides(fresh.Network).Intersect(second.DeliveredSides(planner.Network)));
    }

    [Fact]
    public void CoverageOlderThanTheRestPeriodIsOpenAgain()
    {
        WalkPlanner fresh = Fixture.Planner();
        WalkPlan first = fresh.Plan(fresh.Start(Fixture.Church), AnHour);
        CampaignCoverage general = Covered("General", first, fresh.Network, Fixture.Today.AddDays(-RouteSettings.Defaults.RestDays - 1));

        WalkPlanner planner = Fixture.Planner([general]);

        Assert.Equal(first.Arcs, planner.Plan(planner.Start(Fixture.Church), AnHour).Arcs);
    }

    [Fact]
    public void ACampaignIgnoresAnotherCampaignsCoverage()
    {
        WalkPlanner fresh = Fixture.Planner();
        WalkPlan first = fresh.Plan(fresh.Start(Fixture.Church), AnHour);
        CampaignCoverage general = Covered("General", first, fresh.Network, Fixture.Today.AddDays(-5));
        CampaignCoverage carols = new("Carols 2026", []);

        WalkPlanner planner = Fixture.Planner([carols]);

        Assert.Equal(first.Arcs, planner.Plan(planner.Start(Fixture.Church), AnHour).Arcs);
        Assert.NotEqual(first.Arcs, Fixture.Planner([general]).Plan(planner.Start(Fixture.Church), AnHour).Arcs);
    }

    [Fact]
    public void CarryingTwoCampaignsAStreetIsStillWorthWalkingIfEitherHasntCoveredIt()
    {
        WalkPlanner fresh = Fixture.Planner();
        WalkPlan first = fresh.Plan(fresh.Start(Fixture.Church), AnHour);
        CampaignCoverage general = Covered("General", first, fresh.Network, Fixture.Today.AddDays(-5));
        CampaignCoverage carols = new("Carols 2026", []);

        WalkPlanner both = Fixture.Planner([general, carols]);
        WalkPlanner bothCovered = Fixture.Planner([general, Covered("Carols 2026", first, fresh.Network, Fixture.Today.AddDays(-2))]);

        Assert.Equal(first.Arcs, both.Plan(both.Start(Fixture.Church), AnHour).Arcs);
        Assert.Empty(first.DeliveredSides(fresh.Network).Intersect(bothCovered.Plan(bothCovered.Start(Fixture.Church), AnHour).DeliveredSides(bothCovered.Network)));
    }

    [Fact]
    public void NoJunkMailIsSkippedForLetterboxesButStillKnockedOnAndHostileIsSkippedForBoth()
    {
        StreetGraph graph = Fixture.AlbanyCreek();
        int street = Enumerable.Range(0, graph.StreetCount).First(s => graph.Street(s).CanDeliver && graph.Homes(s, Side.Left) >= 4);
        LetterboxRange side = graph.Letterboxes(street, Side.Left);
        uint noJunkMail = graph.LetterboxAddressKey(side.Start), hostile = graph.LetterboxAddressKey(side.Start + 1);
        OutreachMarks marks = new() { NoJunkMail = new HashSet<uint> { noJunkMail }, Hostile = new HashSet<uint> { hostile } };
        WalkNetwork network = new(graph);

        Openness drop = new(network, [], Fixture.Today, RouteSettings.Defaults, WalkKind.Letterbox, marks);
        Openness knock = new(network, [], Fixture.Today, RouteSettings.Defaults, WalkKind.Doorknock, marks);

        Assert.Equal(graph.Homes(street, Side.Left) - 2, drop.Open(street, Side.Left));
        Assert.Equal(graph.Homes(street, Side.Left) - 1, knock.Open(street, Side.Left));
        Assert.Equal(LetterboxState.Hostile, knock.State(side.Start + 1));
    }

    [Fact]
    public void StreetsInsideAnExcludedZoneAreWalkedThroughButNeverDelivered()
    {
        WalkPlanner fresh = Fixture.Planner();
        WalkPlan first = fresh.Plan(fresh.Start(Fixture.Church), AnHour);
        StreetGraph graph = fresh.Network.Graph;
        List<Flat> points = first.Pieces.Select(fresh.Network.Midpoint).ToList();
        Flat low = new(points.Min(p => p.X), points.Min(p => p.Y)), high = new(points.Max(p => p.X), points.Max(p => p.Y));
        OutreachMarks marks = new()
        {
            ExcludedZones = [[graph.Projection.ToGeo(low), graph.Projection.ToGeo(new Flat(high.X, low.Y)), graph.Projection.ToGeo(high), graph.Projection.ToGeo(new Flat(low.X, high.Y))]],
        };

        WalkPlanner planner = Fixture.Planner(marks: marks);
        WalkPlan plan = planner.Plan(planner.Start(Fixture.Church), AnHour);

        Assert.DoesNotContain(plan.Deliveries.SelectMany(l => l.Arcs), arc => Geometry.Inside([low, new Flat(high.X, low.Y), high, new Flat(low.X, high.Y)], planner.Network.Midpoint(arc.Piece)));
    }

    internal static CampaignCoverage Covered(string campaign, WalkPlan plan, WalkNetwork network, DateOnly walked) =>
        new(campaign, plan.DeliveredSides(network).Select(s => new CoveredStretch(s, walked)).ToList());
}
