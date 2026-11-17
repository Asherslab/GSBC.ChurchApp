using GSBC.ChurchApp.Features.Outreach.Client.Planning;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Common.Printing;
using GeoPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class DayPlannerTests
{
    [Fact]
    public void ADayLeftHalfPlannedComesBackWithTheSameRoutesAndTheCodesTheyWerePublishedUnder()
    {
        DayPlanner planner = Planned();
        planner.Apply(planner.Draft.Rename(planner.Sections[0].Id, "Kingfisher"));
        planner.Published(planner.ToRoutes().Select((route, i) => new DayRoute { Id = $"route-{i}" }).ToList());

        DayPlanner again = Planner();
        again.Restore(Wire(planner.Save()));

        Assert.Equal(planner.Sections.Select(s => (s.Name, s.Letterboxes, s.Colour)), again.Sections.Select(s => (s.Name, s.Letterboxes, s.Colour)));
        Assert.Equal(planner.ToRoutes().Select(r => r.Id), again.ToRoutes().Select(r => r.Id));
        Assert.Equal(planner.Areas.Select(a => (a.Id, a.OneRoute, a.Outline.Count)), again.Areas.Select(a => (a.Id, a.OneRoute, a.Outline.Count)));
    }

    [Fact]
    public void ADayPlannedBeforeItCouldHaveSeveralAreasComesBackAsOneAreaSplitByTheApp()
    {
        DayPlanner planner = Planned();
        DayDraft saved = Wire(planner.Save());
        DayDraft old = new()
        {
            Area = saved.Areas[0].Outline, AreaId = "saved-area", Target = saved.Target, PerRoute = saved.PerRoute, Region = saved.Region,
            Routes = saved.Routes.Select(route => { route.Area = 0; return route; }).ToList(),
        };

        DayPlanner again = Planner();
        again.Restore(Wire(old));

        DraftArea area = Assert.Single(again.Areas);
        Assert.False(area.OneRoute);
        Assert.Equal("saved-area", area.SavedId);
        Assert.All(again.Sections, s => Assert.Equal(area.Id, s.Area));
        Assert.Equal(planner.Sections.Select(s => s.Letterboxes), again.Sections.Select(s => s.Letterboxes));
        Assert.True(again.Regenerate());
    }

    [Fact]
    public void ADayWithASplitAreaAndARouteDrawnByHandPublishesEachLetterboxOnceAndKeepsTheHandDrawnRouteWhenRegenerated()
    {
        DayPlanner planner = Planned();
        int before = planner.Letterboxes;
        Assert.True(planner.AddArea(Square(planner, Fixture.Church, 150), oneRoute: true));
        PlannedSection drawn = Assert.Single(planner.Sections, planner.MadeByHand);

        Assert.True(planner.SizeRoutes(PlanTarget.Letterboxes, 60));

        IReadOnlyList<DayRoute> routes = planner.ToRoutes();
        List<(string Street, StreetSide Side, int From, int To)> sides = routes.SelectMany(r => r.Stretches).Select(s => (s.Street, s.Side, s.From, s.To)).ToList();
        Assert.Equal(sides.Count, sides.Distinct().Count());
        Assert.Equal(before, planner.Letterboxes);
        Assert.Equal(drawn.Letterboxes, planner.Section(drawn.Id)!.Letterboxes);
        AreaOverlap overlap = Assert.Single(planner.Overlaps);
        Assert.Equal((1, drawn.Id), (overlap.Area.Id, overlap.KeptBy.Id));
    }

    [Fact]
    public void PublishedRoutesDeliverEachLetterboxInTheAreaOnce()
    {
        DayPlanner planner = Planned();

        IReadOnlyList<DayRoute> routes = planner.ToRoutes();

        List<(string Street, StreetSide Side, int From, int To)> sides = routes.SelectMany(r => r.Stretches).Select(s => (s.Street, s.Side, s.From, s.To)).ToList();
        Assert.Equal(sides.Count, sides.Distinct().Count());
        Assert.Equal(planner.Letterboxes, routes.Sum(r => r.Stretches.Sum(s => s.Letterboxes)));
        Assert.All(routes, r => Assert.True(OutreachRules.IsValid(r.Stretches[0])));
        Assert.Equal(Enumerable.Range(1, routes.Count), routes.Select(r => r.Number));
    }

    [Fact]
    public void PublishedRoutesComeBackAsADraftThatCanBeEditedWithTheSameRoutes()
    {
        DayPlanner planner = Planned();
        List<DayRoute> published = planner.ToRoutes().Select((route, i) => { route.Id = $"route-{i}"; return route; }).ToList();

        DayPlanner again = Planner();
        again.Restore(DayPlanner.DraftOf(published));

        Assert.Equal(planner.Sections.Select(s => s.Letterboxes), again.Sections.Select(s => s.Letterboxes));
        Assert.Equal(published.Select(route => route.Id), again.ToRoutes().Select(route => route.Id));
    }

    [Fact]
    public void ThePrintedMapsAreOneA4PageARoute()
    {
        DayPlanner planner = Planned();
        List<SheetRoute> routes = planner.Sections.Select(s => new SheetRoute(s, $"CODE{s.Number:00}", $"https://app.example/open/outreach/r/CODE{s.Number:00}")).ToList();

        PdfDocument document = RouteSheets.Make(planner, new SheetDay("Outreach Saturday 6 December", "8:30 to 11:30am", "Church", "General outreach", "Tracts", "", "© OpenStreetMap contributors"), routes);

        Assert.Equal(routes.Count, document.Pages.Count);
        Assert.All(document.Pages, page => Assert.Equal((PdfDocument.A4Width, PdfDocument.A4Height), (page.Width, page.Height)));
    }

    private static DayPlanner Planned()
    {
        DayPlanner planner = Planner();
        Assert.True(planner.AddArea(Square(planner, Fixture.Church, 500), oneRoute: false));
        Assert.InRange(planner.Sections.Count, 3, 40);
        return planner;
    }

    private static List<GeoPoint> Square(DayPlanner planner, GeoPoint around, double half)
    {
        Flat centre = planner.Network.Graph.Projection.ToFlat(around);
        return new[] { new Flat(-half, -half), new Flat(half, -half), new Flat(half, half), new Flat(-half, half) }
            .Select(f => planner.Network.Graph.Projection.ToGeo(centre + f)).ToList();
    }

    private static DayPlanner Planner() =>
        new(Fixture.AlbanyCreek(), "albany-creek", 1, RouteSettings.Defaults, [], OutreachMarks.None, new DateOnly(2026, 12, 6), WalkKind.Letterbox, Fixture.Church);

    private static DayDraft Wire(DayDraft draft)
    {
        using MemoryStream stream = new();
        OutreachContractsModel.Instance.Serialize(stream, draft);
        stream.Position = 0;
        return OutreachContractsModel.Instance.Deserialize<DayDraft>(stream);
    }
}
