using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Routing.Tests;

public sealed class RouteSummaryTests
{
    [Fact]
    public void TheSummaryListsEachStepInOrderWithItsLetterboxesAndTheStreetsWithTheirSides()
    {
        WalkPlanner planner = Fixture.Planner();
        WalkPlan plan = planner.Plan(planner.Start(Fixture.Church), new WalkBudget(Minutes: 60));

        RouteSummary summary = RouteSummary.Of(plan, planner.Open);

        Assert.Equal(plan.Legs.Count, summary.Steps.Count);
        Assert.Equal(Enumerable.Range(1, summary.Steps.Count), summary.Steps.Select(s => s.Number));
        Assert.Equal(plan.Letterboxes, summary.Steps.Sum(s => s.Letterboxes));
        Assert.Equal(plan.Letterboxes, summary.Streets.Sum(s => s.Letterboxes));
        Assert.Equal(summary.Streets.Select(s => s.Title).Distinct().Count(), summary.Streets.Count);
        Assert.All(summary.Streets, s => Assert.InRange(s.Sides, 1, 2));
        Assert.Equal(plan.Time.TotalSeconds, summary.Steps.Sum(s => s.Time.TotalSeconds), 3);
        Assert.Equal(summary.Steps[0].Line[0], summary.Line[0]);
    }

    [Fact]
    public void NonHomesAndDontLetterboxAddressesAreMarkedOnTheirStepWithoutSayingWhy()
    {
        StreetGraph graph = Fixture.AlbanyCreek();
        WalkNetwork network = new(graph);
        WalkPlanner bare = new(new Openness(network, [], Fixture.Today, RouteSettings.Defaults));
        int start = bare.Start(Fixture.Church);
        WalkPlan first = bare.Plan(start, new WalkBudget(Minutes: 90));
        int box = RouteSummary.Of(first, bare.Open).Delivered
            .Select(s => graph.Between(s.Street, s.Side, s.From, s.To))
            .Where(r => r.Count >= 4)
            .SelectMany(r => Enumerable.Range(r.Start, r.Count))
            .First(b => graph.LetterboxKindOf(b) == LetterboxKind.House);
        uint key = graph.LetterboxAddressKey(box);
        WalkPlanner planner = new(new Openness(network, [], Fixture.Today, RouteSettings.Defaults, marks: new OutreachMarks
        {
            Excluded = new HashSet<uint> { key },
            ShownToWalkers = new HashSet<uint> { key },
        }));

        RouteSummary summary = RouteSummary.Of(planner.Summarise(first.Start, first.Arcs, new HashSet<int>()), planner.Open);

        RouteMarker marker = Assert.Single(summary.Steps.SelectMany(s => s.Markers), m => m.Kind == MarkerKind.DontLetterbox);
        Assert.Equal("Don't letterbox", marker.Label);
        Assert.All(summary.Steps.SelectMany(s => s.Markers).Where(m => m.Kind == MarkerKind.Skip), m => Assert.StartsWith("Skip: ", m.Label));
        Assert.Equal(summary.Steps.SelectMany(s => s.Markers).Count(m => m.Kind == MarkerKind.Skip), summary.SkippedNonHomes);
    }
}
