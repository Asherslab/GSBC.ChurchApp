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
        Assert.Equal(planner.Area, again.Area);
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
        Flat centre = planner.Network.Graph.Projection.ToFlat(Fixture.Church);
        Assert.True(planner.Draw(new[] { new Flat(-500, -500), new Flat(500, -500), new Flat(500, 500), new Flat(-500, 500) }
            .Select(f => planner.Network.Graph.Projection.ToGeo(centre + f)).ToList()));
        Assert.InRange(planner.Sections.Count, 3, 40);
        return planner;
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
