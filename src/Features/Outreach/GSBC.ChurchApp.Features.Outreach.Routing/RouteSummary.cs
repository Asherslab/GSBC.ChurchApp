namespace GSBC.ChurchApp.Features.Outreach.Routing;

public enum MarkerKind
{
    Skip,
    Check,
    NoJunkMail,
    DontLetterbox,
}

public sealed record RouteMarker(GeoPoint Point, MarkerKind Kind, string Label, double Along);

public sealed record RouteStep(
    int Number,
    string Street,
    string Title,
    bool Delivers,
    int Letterboxes,
    double Metres,
    TimeSpan Time,
    IReadOnlyList<GeoPoint> Line,
    IReadOnlyList<SideStretch> Sides,
    IReadOnlyList<RouteMarker> Markers);

public sealed record StreetLine(string Title, int Sides, int Letterboxes);

public sealed record RouteSummary(
    double Metres,
    int Letterboxes,
    int Bundles,
    int BundlesToTake,
    TimeSpan Time,
    double ConnectorMetres,
    int SkippedNonHomes,
    bool NeedsReview,
    IReadOnlyList<RouteStep> Steps,
    IReadOnlyList<StreetLine> Streets,
    IReadOnlyList<GeoPoint> Line)
{
    private const double MarkerSetbackMetres = 12;

    public IEnumerable<SideStretch> Delivered => Steps.Where(s => s.Delivers).SelectMany(s => s.Sides);

    public static RouteSummary Of(WalkPlan plan, Openness open)
    {
        WalkNetwork network = open.Network;
        List<RouteStep> steps = [];
        List<GeoPoint> whole = [];
        int skipped = 0;
        foreach (WalkLeg leg in plan.Legs)
        {
            List<GeoPoint> line = [];
            List<SideStretch> sides = [];
            List<RouteMarker> markers = [];
            double seconds = 0;
            foreach (Arc arc in leg.Arcs)
            {
                Piece piece = network[arc.Piece];
                IReadOnlyList<GeoPoint> points = network.Line(arc.Piece, arc.Forward);
                line.AddRange(line.Count > 0 ? points.Skip(1) : points);
                seconds += open.WalkSeconds(piece.Length);
                if (!leg.Delivers)
                    continue;
                seconds += open.DeliverySeconds(arc.Piece, arc.Side);
                sides.Add(new SideStretch(piece.Street, arc.Side, piece.From, piece.To));
                IEnumerable<RouteMarker> found = Markers(open, arc);
                markers.AddRange(arc.Forward ? found : found.Reverse());
            }
            skipped += markers.Count(m => m.Kind == MarkerKind.Skip);
            whole.AddRange(whole.Count > 0 ? line.Skip(1) : line);
            steps.Add(new RouteStep(steps.Count + 1, leg.Street, leg.Title, leg.Delivers, leg.Letterboxes, leg.Metres, TimeSpan.FromSeconds(seconds), line, sides, markers));
        }
        List<StreetLine> streets = steps.Where(s => s.Delivers)
            .GroupBy(s => s.Title)
            .Select(g => new StreetLine(g.Key, g.SelectMany(s => s.Sides).Select(x => x.Side).Distinct().Count(), g.Sum(s => s.Letterboxes)))
            .ToList();
        return new RouteSummary(plan.Metres, plan.Letterboxes, plan.Bundles, plan.BundlesToTake, plan.Time, plan.ConnectorMetres, skipped, plan.NeedsReview, steps, streets, whole);
    }

    private static IEnumerable<RouteMarker> Markers(Openness open, Arc arc)
    {
        StreetGraph graph = open.Network.Graph;
        LetterboxRange range = open.Network.Letterboxes(arc.Piece, arc.Side);
        for (int b = range.Start; b < range.End; b++)
        {
            uint key = graph.LetterboxAddressKey(b);
            MarkerKind? kind = open.State(b) switch
            {
                LetterboxState.NotAHome => MarkerKind.Skip,
                LetterboxState.NoJunkMail => MarkerKind.NoJunkMail,
                LetterboxState.Hostile => MarkerKind.DontLetterbox,
                LetterboxState.Excluded when key != 0 && open.Marks.ShownToWalkers.Contains(key) => MarkerKind.DontLetterbox,
                LetterboxState.Open when graph.LetterboxKindOf(b) == LetterboxKind.Check => MarkerKind.Check,
                _ => null,
            };
            if (kind is not { } marker)
                continue;
            string label = marker switch
            {
                MarkerKind.Skip => $"Skip: {graph.LetterboxLabel(b) ?? "not a home"}",
                MarkerKind.NoJunkMail => "No junk mail",
                MarkerKind.DontLetterbox => "Don't letterbox",
                _ => "Check: may not be a home",
            };
            yield return new RouteMarker(graph.Projection.ToGeo(graph.LetterboxFlat(b, MarkerSetbackMetres)), marker, label, graph.LetterboxAlong(b));
        }
    }
}
