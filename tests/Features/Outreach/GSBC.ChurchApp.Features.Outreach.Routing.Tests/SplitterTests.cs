namespace GSBC.ChurchApp.Features.Outreach.Routing.Tests;

public sealed class SplitterTests
{
    private const double PerRoute = 120;
    private const int Colours = 8;

    [Fact]
    public void AnAreaSplitsIntoAboutTheRightNumberOfEvenConnectedRoutes()
    {
        (AreaSplitter splitter, AreaSelection selection, RouteDraft draft) = Drafted();

        int total = selection.Fresh.Sum(splitter.Planner.Open.Open);
        Assert.True(draft.Sections.Count >= total / PerRoute * 0.7);
        Assert.Equal(selection.Fresh.Order(), draft.Sections.SelectMany(s => s.Pieces).Order());
        List<SectionSummary> summaries = draft.Summaries(Fixture.Church).ToList();
        Assert.All(summaries, s => Assert.False(s.InPieces, $"{s.Name} is in pieces"));
        IEnumerable<SectionSummary> joinedToOthers = summaries.Where(s => draft.Sections.Any(o => o.Id != s.Section.Id && o.Pieces.Any(p => s.Section.Pieces.Any(q => draft.Graph!.Touch(p, q)))));
        Assert.All(joinedToOthers, s => Assert.InRange(s.Letterboxes, PerRoute * 0.4, PerRoute * 1.8));
    }

    [Fact]
    public void ACutOffGroupBecomesItsOwnRouteRatherThanALongEmptyWalk()
    {
        (_, _, RouteDraft draft) = Drafted(Fixture.BesideEatonsCrossingRd, 900);

        List<SectionSummary> summaries = draft.Summaries(Fixture.BesideEatonsCrossingRd).ToList();

        Assert.All(summaries, s => Assert.True(s.Plan.LongestConnectorMetres <= RouteSettings.Defaults.MaxConnectorLegMetres + RouteSettings.Defaults.SplitJoinMetres,
            $"{s.Name} walks {s.Plan.LongestConnectorMetres:0} m without letterboxes"));
    }

    [Fact]
    public void EditsLeaveTheDraftTheyStartedFromAsItWasAndUndoAndRedoStepThroughThem()
    {
        (_, _, RouteDraft draft) = Drafted();
        int[] two = [draft.Sections[0].Id, draft.Sections[1].Id];

        DraftEdit merged = draft.Merge(two);

        Assert.True(merged.Changed);
        Assert.Equal(draft.Sections.Count - 1, merged.Draft.Sections.Count);
        Assert.Equal(draft.Sections[0].Pieces.Union(draft.Sections[1].Pieces).Order(), merged.Draft.Section(two[0])!.Pieces.Order());
        Assert.NotNull(draft.Section(two[1]));
        Assert.Equal(draft.Sections, merged.Draft.Undo().Sections);
        Assert.Equal(merged.Draft.Sections, merged.Draft.Undo().Redo().Sections);
        Assert.False(merged.Draft.Undo().Merge(two).Draft.CanRedo);
    }

    [Fact]
    public void PaintingPartOfAStreetMovesJustThatStretchAndPaintingItBackJoinsTheStreetUp()
    {
        (AreaSplitter splitter, _, RouteDraft draft) = Drafted();
        WalkNetwork network = splitter.Planner.Network;
        DraftSection from = draft.Sections.First(s => s.Pieces.Any(p => network.Graph.Homes(p, Side.Left) >= 6));
        DraftSection into = draft.Sections.First(s => s.Id != from.Id);
        int street = from.Pieces.First(p => network.Graph.Homes(p, Side.Left) >= 6);

        RouteDraft painted = draft.Paint(into.Id, [new Stretch(street, 0, 0.5)]).Draft;

        Assert.Contains(painted.Section(into.Id)!.Pieces, p => network[p] is { Street: var s, From: 0, To: 0.5 } && s == street);
        Assert.Contains(painted.Section(from.Id)!.Pieces, p => network[p] is { Street: var s, From: 0.5, To: 1 } && s == street);
        RouteDraft back = painted.Paint(from.Id, [new Stretch(street, 0, 0.5)]).Draft;
        Assert.Contains(street, back.Section(from.Id)!.Pieces);
    }

    [Fact]
    public void ALockedRouteKeepsItsStreetsThroughPaintingAndRegenerating()
    {
        (_, _, RouteDraft draft) = Drafted();
        DraftSection locked = draft.Sections[0];
        RouteDraft held = draft.SetLocked([locked.Id], true).Draft;

        DraftEdit paint = held.Paint(locked.Id, [Stretch.Whole(draft.Sections[1].Pieces.First())]);
        DraftEdit steal = held.Paint(draft.Sections[1].Id, [Stretch.Whole(locked.Pieces.First())]);
        RouteDraft regenerated = held.Regenerate(SplitTarget.Letterboxes, PerRoute * 2).Draft;

        Assert.False(paint.Changed);
        Assert.Contains("locked", paint.Note);
        Assert.False(steal.Changed);
        Assert.Equal(locked.Pieces.Order(), regenerated.Section(locked.Id)!.Pieces.Order());
    }

    [Fact]
    public void ScissorsAcrossARouteSplitItWhereTheLineCrossesItsStreets()
    {
        (AreaSplitter splitter, _, RouteDraft draft) = Drafted();
        WalkNetwork network = splitter.Planner.Network;
        (DraftSection section, int piece) = draft.Sections
            .SelectMany(s => s.Pieces.Order().Select(p => (Section: s, Piece: p)))
            .First(c => network[c.Piece].Length > 60 && draft.Graph!.Components(c.Section.Pieces.Where(p => p != c.Piece).ToList()).Count > 1);

        DraftEdit cut = draft.Cut(Across(network, piece, 30));

        Assert.True(cut.Changed, cut.Note);
        Assert.True(cut.Draft.Sections.Count > draft.Sections.Count);
        Assert.Equal(section.Pieces.Order(), cut.Draft.Sections.Where(s => s.Pieces.All(section.Pieces.Contains)).SelectMany(s => s.Pieces).Order());
        Assert.Equal($"Route {draft.Number(section.Id)}a", cut.Draft.Section(section.Id)!.Name);
    }

    [Fact]
    public void ScissorsThatMissEveryRouteSaySo()
    {
        (AreaSplitter splitter, _, RouteDraft draft) = Drafted();
        StreetGraph graph = splitter.Planner.Network.Graph;

        DraftEdit cut = draft.Cut([graph.Projection.ToGeo(new Flat(-20000, -20000)), graph.Projection.ToGeo(new Flat(-19000, -20000))]);

        Assert.False(cut.Changed);
        Assert.Contains("doesn't cross", cut.Note);
    }

    [Fact]
    public void ALassoTakesTheStreetsInsideIntoANewRoute()
    {
        (AreaSplitter splitter, _, RouteDraft draft) = Drafted();
        Flat centre = splitter.Planner.Network.Graph.Projection.ToFlat(Fixture.Church);
        GeoPoint[] loop = new[] { new Flat(-150, -150), new Flat(150, -150), new Flat(150, 150), new Flat(-150, 150) }
            .Select(f => splitter.Planner.Network.Graph.Projection.ToGeo(centre + f)).ToArray();

        DraftEdit lasso = draft.Lasso(null, loop);

        Assert.True(lasso.Changed);
        DraftSection added = lasso.Draft.Sections.Single(s => draft.Section(s.Id) is null);
        Assert.All(added.Pieces, p => Assert.True(Geometry.Inside(loop.Select(splitter.Planner.Network.Graph.Projection.ToFlat).ToList(), splitter.Planner.Network.Midpoint(p))));
    }

    [Fact]
    public void ErasingAStreetLeavesItOutAndRebalanceEvensTheRest()
    {
        (_, _, RouteDraft draft) = Drafted();
        int street = draft.Sections[0].Pieces.First();

        RouteDraft erased = draft.Erase([Stretch.Whole(street)]).Draft;
        DraftEdit rebalanced = erased.Rebalance(SplitTarget.Letterboxes);

        Assert.Null(erased.OwnerOf(street));
        Assert.Null(rebalanced.Draft.OwnerOf(street));
    }

    [Fact]
    public void ABrushStrokeAlongAStreetSnapsItsEndsToCornersNearby()
    {
        (AreaSplitter splitter, _, RouteDraft draft) = Drafted();
        WalkNetwork network = splitter.Planner.Network;
        int street = draft.Streets.Where(s => network[s].Length > 150).Order().First();
        GeoPoint[] stroke = new[] { 0.04, 0.3, 0.6 }.Select(at => network.Graph.Projection.ToGeo(network.Graph.PointAt(street, at))).ToArray();

        Stretch stretch = Assert.Single(draft.Brush(stroke));

        Assert.Equal(street, stretch.Street);
        Assert.Equal(0, stretch.From);
        Assert.Equal(0.6, stretch.To, 2);
    }

    [Fact]
    public void ADraftSavedAsStretchesRestoresToTheSameRoutesWithTheirNamesLocksAndStarts()
    {
        (AreaSplitter splitter, _, RouteDraft draft) = Drafted();
        WalkNetwork network = splitter.Planner.Network;
        draft = draft.Paint(draft.Sections[1].Id, [new Stretch(draft.Sections[0].Pieces.First(p => network.Graph.Homes(p) >= 6), 0, 0.5)]).Draft;
        draft = draft.Rename(draft.Sections[0].Id, "Kingfisher").Draft.SetLocked([draft.Sections[1].Id], true).Draft;
        RouteDraft restored = RouteDraft.Restore(splitter, Colours, draft.Areas, Saved(draft, network));

        Assert.Equal(draft.Sections.Select(s => (s.Id, s.Name, s.Colour, s.Locked)), restored.Sections.Select(s => (s.Id, s.Name, s.Colour, s.Locked)));
        Assert.Equal(draft.Summaries(Fixture.Church).Select(s => s.Letterboxes), restored.Summaries(Fixture.Church).Select(s => s.Letterboxes));
        Assert.True(restored.Merge([restored.Sections[2].Id, restored.Sections[3].Id]).Changed);
    }

    [Fact]
    public void AHandDrawnRouteTakesItsStreetsFromASplitAreaAndKeepsThemThroughRegenerateAndRebalance()
    {
        (AreaSplitter splitter, _, RouteDraft draft) = Drafted();
        DraftArea outline = Area(splitter, 2, oneRoute: true, Fixture.Church, 150);
        HashSet<int> inside = outline.Streets.Where(s => splitter.Planner.Open.Open(s) > 0).ToHashSet();

        RouteDraft drawn = draft.AddArea(outline, SplitTarget.Letterboxes, PerRoute).Draft;
        DraftSection route = drawn.Sections.Single(s => s.Area == outline.Id);
        RouteDraft reworked = drawn.Regenerate(SplitTarget.Letterboxes, PerRoute / 2).Draft.Rebalance(SplitTarget.Letterboxes).Draft;

        Assert.True(drawn.MadeByHand(route));
        Assert.Equal(inside.Order(), route.Pieces.Order());
        Assert.All(drawn.Sections.Where(s => s.Id != route.Id), s => Assert.DoesNotContain(s.Pieces, inside.Contains));
        Assert.Equal(route.Pieces.Order(), reworked.Section(route.Id)!.Pieces.Order());
        Assert.Equal(draft.Streets.Where(s => splitter.Planner.Open.Open(s) > 0).Order(), reworked.Sections.SelectMany(s => s.Pieces).Order());
    }

    [Fact]
    public void WhereTwoAreasOverlapEachStreetIsInOneRouteAndAnEarlierHandDrawnRouteKeepsItsOwn()
    {
        (AreaSplitter splitter, _, RouteDraft draft) = Drafted();
        DraftArea first = Area(splitter, 2, oneRoute: true, Fixture.Church, 150);
        DraftArea second = Area(splitter, 3, oneRoute: true, Offset(splitter, Fixture.Church, 120), 150);
        DraftArea split = Area(splitter, 4, oneRoute: false, Offset(splitter, Fixture.Church, 400), 500);

        RouteDraft drafted = draft.AddArea(first, SplitTarget.Letterboxes, PerRoute).Draft
            .AddArea(second, SplitTarget.Letterboxes, PerRoute).Draft
            .AddArea(split, SplitTarget.Letterboxes, PerRoute).Draft;

        List<int> pieces = drafted.Sections.SelectMany(s => s.Pieces).ToList();
        Assert.Equal(pieces.Count, pieces.Distinct().Count());
        HashSet<int> shared = first.Streets.Intersect(second.Streets).Where(s => splitter.Planner.Open.Open(s) > 0).ToHashSet();
        Assert.NotEmpty(shared);
        Assert.Superset(shared, drafted.Sections.Single(s => s.Area == first.Id).Pieces.ToHashSet());
        Assert.DoesNotContain(drafted.Sections.Single(s => s.Area == second.Id).Pieces, shared.Contains);
    }

    [Fact]
    public void TakingAnAreaOffTheDayTakesItsRoutesAndUndoBringsThemBack()
    {
        (AreaSplitter splitter, _, RouteDraft draft) = Drafted();
        DraftArea outline = Area(splitter, 2, oneRoute: true, Offset(splitter, Fixture.Church, 1400), 150);
        RouteDraft drawn = draft.AddArea(outline, SplitTarget.Letterboxes, PerRoute).Draft;

        RouteDraft removed = drawn.RemoveArea(outline.Id).Draft;

        Assert.Null(removed.Area(outline.Id));
        Assert.DoesNotContain(removed.Sections, s => s.Area == outline.Id);
        Assert.Equal(Shape(draft), Shape(removed));
        Assert.Equal(Shape(drawn), Shape(removed.Undo()));
        Assert.Equal(drawn.Areas, removed.Undo().Areas);
    }

    [Fact]
    public void RedrawingAHandDrawnRouteKeepsItsNumberAndNameSoItsPrintedCodeStillOpensIt()
    {
        (AreaSplitter splitter, _, RouteDraft draft) = Drafted();
        DraftArea outline = Area(splitter, 2, oneRoute: true, Fixture.Church, 150);
        RouteDraft drawn = draft.AddArea(outline, SplitTarget.Letterboxes, PerRoute).Draft;
        DraftSection route = drawn.Sections.Single(s => s.Area == outline.Id);
        drawn = drawn.Rename(route.Id, "Church block").Draft;

        RouteDraft redrawn = drawn.ReplaceArea(Area(splitter, 2, oneRoute: true, Fixture.Church, 220), SplitTarget.Letterboxes, PerRoute).Draft;
        DraftSection again = redrawn.Sections.Single(s => s.Area == outline.Id);

        Assert.Equal((route.Id, route.Colour, "Church block"), (again.Id, again.Colour, again.Name));
        Assert.True(again.Pieces.Count > route.Pieces.Count);
    }

    [Fact]
    public void SwitchingAnAreaToOneRouteAndBackOnlyChangesThatArea()
    {
        (AreaSplitter splitter, _, RouteDraft draft) = Drafted();
        DraftArea near = Area(splitter, 2, oneRoute: false, Offset(splitter, Fixture.Church, 1500), 300);
        RouteDraft both = draft.AddArea(near, SplitTarget.Letterboxes, PerRoute).Draft;

        RouteDraft one = both.ReplaceArea(near with { OneRoute = true }, SplitTarget.Letterboxes, PerRoute).Draft;

        Assert.Single(one.Sections, s => s.Area == near.Id);
        Assert.Equal(both.Sections.Where(s => s.Area == near.Id).SelectMany(s => s.Pieces).Order(), one.Sections.Single(s => s.Area == near.Id).Pieces.Order());
        Assert.Equal(Shape(both).Where(s => s.Area == 1), Shape(one).Where(s => s.Area == 1));
    }

    private static List<(int Id, int Area, string Pieces)> Shape(RouteDraft draft) =>
        draft.Sections.Select(s => (s.Id, s.Area, string.Join(",", s.Pieces.Order()))).ToList();

    private static List<SavedSection> Saved(RouteDraft draft, WalkNetwork network) =>
        draft.Sections.Select(s => new SavedSection(s.Id, [.. s.Stretches(network)], s.Name, s.Colour, s.Locked,
            s.Start is { } node ? network.Graph.Projection.ToGeo(network.Node(node)) : null, s.Area)).ToList();

    private static GeoPoint Offset(AreaSplitter splitter, GeoPoint from, double east)
    {
        LocalProjection projection = splitter.Planner.Network.Graph.Projection;
        return projection.ToGeo(projection.ToFlat(from) + new Flat(east, 0));
    }

    private static DraftArea Area(AreaSplitter splitter, int id, bool oneRoute, GeoPoint around, double half)
    {
        LocalProjection projection = splitter.Planner.Network.Graph.Projection;
        Flat centre = projection.ToFlat(around);
        GeoPoint[] polygon = new[] { new Flat(-half, -half), new Flat(half, -half), new Flat(half, half), new Flat(-half, half) }
            .Select(f => projection.ToGeo(centre + f)).ToArray();
        AreaSelection selection = splitter.Select(polygon);
        return new DraftArea(id, oneRoute, selection.Fresh, [.. selection.Connectors(false)], polygon);
    }

    private static GeoPoint[] Across(WalkNetwork network, int piece, double metres)
    {
        Flat middle = network.Midpoint(piece);
        double heading = network.Graph.HeadingAt(network[piece].Street, (network[piece].From + network[piece].To) / 2) + Math.PI / 2;
        Flat across = new(Math.Cos(heading) * metres, Math.Sin(heading) * metres);
        return [network.Graph.Projection.ToGeo(middle - across), network.Graph.Projection.ToGeo(middle + across)];
    }

    private static (AreaSplitter, AreaSelection, RouteDraft) Drafted(GeoPoint? around = null, double half = 600)
    {
        WalkPlanner planner = Fixture.Planner();
        AreaSplitter splitter = new(planner);
        Flat centre = planner.Network.Graph.Projection.ToFlat(around ?? Fixture.Church);
        GeoPoint[] polygon = new[] { new Flat(-half, -half), new Flat(half, -half), new Flat(half, half), new Flat(-half, half) }
            .Select(f => planner.Network.Graph.Projection.ToGeo(centre + f)).ToArray();
        AreaSelection selection = splitter.Select(polygon);
        DraftArea area = new(1, false, selection.Fresh, [.. selection.Connectors(false)], polygon);
        RouteDraft draft = RouteDraft.Empty(splitter, Colours).AddArea(area, SplitTarget.Letterboxes, PerRoute).Draft;
        return (splitter, selection, draft);
    }
}
