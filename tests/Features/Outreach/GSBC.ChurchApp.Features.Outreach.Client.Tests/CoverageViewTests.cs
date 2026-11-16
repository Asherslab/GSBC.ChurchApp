using GSBC.ChurchApp.Features.Outreach.Admin;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Ui;
using GSBC.ChurchApp.Platform.Ui.Maps;
using RecordStretch = GSBC.ChurchApp.Features.Outreach.Contracts.Stretch;
using CoveredStretch = GSBC.ChurchApp.Features.Outreach.Contracts.CoveredStretch;
using GeoPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class CoverageViewTests
{
    private static readonly DateOnly Today = new(2026, 11, 16);
    private const int RestDays = 84;

    [Fact]
    public async Task TheFiguresForAViewCountEveryDeliverableStreetWhoseMiddleIsInIt()
    {
        StreetGraph graph = Fixture.AlbanyCreek();
        List<StreetCoverage> coverage = Covered(graph);
        CoverageView view = await Build(graph, coverage, _ => true);

        foreach (MapFrame frame in Frames(graph))
            Assert.Equal(Counted(graph, coverage, _ => true, frame), view.TallyIn(frame));
    }

    [Fact]
    public async Task WhereTwoRegionsShareStreetsEachStreetIsCountedAndDrawnOnce()
    {
        StreetGraph graph = Fixture.AlbanyCreek();
        List<StreetCoverage> coverage = Covered(graph);
        double split = (graph.Region.Bounds.West + graph.Region.Bounds.East) / 2;
        CoverageView whole = await Build(graph, coverage, _ => true);
        CoverageView west = await Build(graph, coverage, point => point.Lon < split);
        CoverageView east = await Build(graph, coverage, point => point.Lon >= split);

        foreach (MapFrame frame in Frames(graph))
            Assert.Equal(whole.TallyIn(frame), west.TallyIn(frame) + east.TallyIn(frame));
        Assert.Equal(whole.Lines.Count, west.Lines.Count + east.Lines.Count);
    }

    private static async Task<CoverageView> Build(StreetGraph graph, List<StreetCoverage> coverage, Func<MapPoint, bool> owns)
    {
        FrameBudget budget = new(new StillTime());
        return await CoverageView.BuildAsync(graph, await CoverageRecord.ReadAsync(coverage, budget), RestDays, Today, owns, budget);
    }

    private static List<StreetCoverage> Covered(StreetGraph graph)
    {
        Random random = new(20261116);
        List<StreetCoverage> streets = [];
        for (int street = 0; street < graph.StreetCount; street++)
        {
            if (random.NextDouble() > 0.4)
                continue;
            int from = random.Next(0, 500);
            streets.Add(new StreetCoverage
            {
                Street = graph.KeyOf(street).ToString(),
                Stretches =
                [
                    new CoveredStretch { Side = StreetSide.Left, From = 0, To = OutreachRules.WholeStreet, At = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new CoveredStretch { Side = StreetSide.Right, From = from, To = from + 400, At = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc) },
                ],
            });
        }
        return streets;
    }

    private static IEnumerable<MapFrame> Frames(StreetGraph graph)
    {
        GeoBounds bounds = graph.Region.Bounds;
        yield return new MapFrame(bounds.South - 1, bounds.West - 1, bounds.North + 1, bounds.East + 1);
        Random random = new(20261117);
        for (int index = 0; index < 200; index++)
        {
            double size = Math.Pow(10, -3 + random.NextDouble() * 2);
            double south = bounds.South + random.NextDouble() * (bounds.North - bounds.South);
            double west = bounds.West + random.NextDouble() * (bounds.East - bounds.West);
            yield return new MapFrame(south, west, south + size, west + size * 1.6);
        }
    }

    private static CoverageTally Counted(StreetGraph graph, List<StreetCoverage> coverage, Func<MapPoint, bool> owns, MapFrame view)
    {
        HashSet<int> shown = [];
        for (int street = 0; street < graph.StreetCount; street++)
        {
            GeoPoint middle = graph.Projection.ToGeo(graph.PointAt(street, graph.Street(street).Length / 2));
            MapPoint point = new(middle.Lat, middle.Lon);
            if (graph.Street(street).CanDeliver && graph.Homes(street) > 0 && owns(point)
                && point.Lat >= view.South && point.Lat < view.North && point.Lon >= view.West && point.Lon < view.East)
                shown.Add(street);
        }
        List<(int Street, int Homes)> done = [.. coverage
            .SelectMany(street => street.Stretches.Select(covered => EngineInputs.SideStretch(graph, new RecordStretch { Street = street.Street, Side = covered.Side, From = covered.From, To = covered.To })))
            .OfType<SideStretch>()
            .Where(stretch => shown.Contains(stretch.Street))
            .Select(stretch => (stretch.Street, Homes(graph, stretch)))];
        return new CoverageTally(shown.Count, done.Select(piece => piece.Street).Distinct().Count(), shown.Sum(graph.Homes), done.Sum(piece => piece.Homes));
    }

    private static int Homes(StreetGraph graph, SideStretch stretch)
    {
        LetterboxRange boxes = graph.Between(stretch.Street, stretch.Side, stretch.From, stretch.To);
        return Enumerable.Range(boxes.Start, boxes.End - boxes.Start).Count(box => graph.LetterboxKindOf(box).IsHome());
    }

    private sealed class StillTime : TimeProvider
    {
        public override long GetTimestamp() => 0;
    }
}
