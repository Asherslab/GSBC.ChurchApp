namespace Gsbc.AppPoc.Data.Outreach;

public sealed record EditOutcome(bool Changed, string? Note = null)
{
    public static readonly EditOutcome Done = new(true);
}

public sealed record CutPiece(HashSet<int> Edges, int Colour);

public sealed record CutPreview(DraftSection Route, HashSet<int> Kept, List<CutPiece> Pieces);

public sealed class RouteDraft(AreaSplitter splitter, RouteEditor editor, StreetNetwork net, LetterboxCount count, int colours)
{
    public const int MaxHistory = 50;
    private const string Locked = "is locked. Unlock it to change its streets.";
    private const double MinStretchMetres = 1;

    private sealed record SectionState(DraftSection Section, int[] Edges, string? Name, int Colour, bool Locked, int? Start);

    private sealed record Segment(double From, double To, DraftSection? Owner);

    private sealed record Snapshot(List<SectionState> Sections, SplitGraph? Graph);

    private readonly List<Snapshot> undo = [];
    private readonly List<Snapshot> redo = [];

    public List<DraftSection> Sections { get; private set; } = [];

    public SplitGraph? Graph { get; private set; }

    public IReadOnlySet<int> Streets { get; private set; } = new HashSet<int>();

    public bool CanUndo => undo.Count > 0;

    public bool CanRedo => redo.Count > 0;

    public void Reset(IEnumerable<int> streets)
    {
        Streets = streets.ToHashSet();
        Sections = [];
        Graph = null;
        undo.Clear();
        redo.Clear();
    }

    public int Number(DraftSection section) => Sections.IndexOf(section) + 1;

    public DraftSection? OwnerOf(int edge) => Sections.FirstOrDefault(s => s.Edges.Contains(edge));

    public IReadOnlyDictionary<int, double[]> Cuts() => net.CutsOf(Sections.SelectMany(s => s.Edges));

    public IEnumerable<int> PiecesOf(int street, IReadOnlyDictionary<int, double[]> cuts) => net.Split(street, cuts.GetValueOrDefault(street) ?? []);

    public EditOutcome Regenerate(IEnumerable<int> edges, SplitTarget target, double perRoute, IEnumerable<int> connectors) => Edit(() =>
    {
        var locked = Sections.Where(s => s.Locked).ToList();
        var lockedStreets = locked.SelectMany(s => s.Edges).Select(e => net[e].Street).ToHashSet();
        var free = edges.SelectMany(e => lockedStreets.Contains(e) ? Unlocked(e) : [e]).ToList();
        var (fresh, graph) = splitter.Split(free, target, perRoute, connectors);
        Sections = [.. locked];
        foreach (var section in fresh)
        {
            section.Colour = NextColour();
            Sections.Add(section);
        }
        Graph = graph;
        return EditOutcome.Done;
    });

    public EditOutcome Paint(DraftSection into, IReadOnlyList<Stretch> stretches) => Edit(() =>
        into.Locked ? new(false, $"Route {Number(into)} {Locked}") : Reassign(stretches, into));

    public EditOutcome Erase(IReadOnlyList<Stretch> stretches) => Edit(() => Reassign(stretches, null));

    public int Moving(DraftSection? into, IReadOnlyList<Stretch> stretches) =>
        Touched(stretches).Where(t => Moves(t.Owner, into)).Sum(t => Letterboxes([net.PieceOf(t.Street, t.From, t.To)]));

    public HashSet<int> Lassoed(DraftSection? into, IReadOnlyList<GeoPoint> loop, RoutePlanner planner)
    {
        if (loop.Count < 3)
        {
            return [];
        }
        var cuts = Cuts();
        return Streets.SelectMany(street => PiecesOf(street, cuts))
            .Where(e => Letterboxes([e]) > 0 && into?.Edges.Contains(e) != true && OwnerOf(e) is not { Locked: true })
            .Where(e => AreaSplitter.Inside(loop, planner.Midpoint(e)))
            .ToHashSet();
    }

    public EditOutcome Lasso(DraftSection? into, IReadOnlyList<GeoPoint> loop, RoutePlanner planner) => Edit(() =>
    {
        if (into is { Locked: true })
        {
            return new(false, $"Route {Number(into)} {Locked}");
        }
        var captured = Lassoed(into, loop, planner);
        if (captured.Count == 0)
        {
            return new(false, "The loop didn't take in any streets with letterboxes.");
        }
        foreach (var edge in captured)
        {
            OwnerOf(edge)?.Edges.Remove(edge);
        }
        if (into is null)
        {
            Sections.Add(new DraftSection(captured, NextColour()));
        }
        else
        {
            into.Edges.UnionWith(captured);
        }
        return EditOutcome.Done;
    });

    public List<CutPreview> PreviewCut(IReadOnlyList<GeoPoint> line)
    {
        var previews = new List<CutPreview>();
        if (Graph is null)
        {
            return previews;
        }
        var used = Sections.Select(s => s.Colour).ToList();
        foreach (var cut in editor.Cut(Graph, Sections, line, count))
        {
            var avoid = new List<int> { cut.Route.Colour };
            avoid.AddRange(Neighbours(cut.Route).Select(n => n.Colour));
            var pieces = new List<CutPiece>();
            foreach (var edges in cut.Pieces)
            {
                var colour = NextColour(used, avoid);
                used.Add(colour);
                avoid.Add(colour);
                pieces.Add(new(edges, colour));
            }
            previews.Add(new(cut.Route, cut.Kept, pieces));
        }
        return previews;
    }

    public bool CutTouches(IReadOnlyList<GeoPoint> line) => Graph is not null && editor.Crosses(Sections, line);

    public EditOutcome Cut(IReadOnlyList<GeoPoint> line) => Edit(() =>
    {
        var previews = PreviewCut(line);
        if (previews.Count == 0)
        {
            return new(false, CutTouches(line)
                ? "The route is still joined elsewhere. Cut across every street between the two parts."
                : "That cut doesn't cross a route. Drag across its streets where the split should be.");
        }
        foreach (var preview in previews)
        {
            var number = Number(preview.Route);
            preview.Route.Edges.IntersectWith(preview.Kept);
            preview.Route.CustomName = $"Route {number}a";
            preview.Route.Start = null;
            var at = Sections.IndexOf(preview.Route);
            foreach (var (piece, i) in preview.Pieces.Select((p, i) => (p, i)))
            {
                Sections.Insert(at + 1 + i, new DraftSection(piece.Edges, piece.Colour) { CustomName = $"Route {number}{(char)('b' + i)}" });
            }
        }
        return EditOutcome.Done;
    });

    public EditOutcome SplitInTwo(DraftSection section, SplitTarget target) => Edit(() =>
    {
        if (section.Locked || section.Edges.Count < 2)
        {
            return new(false, section.Locked ? $"Route {Number(section)} {Locked}" : null);
        }
        var halves = splitter.SplitInTwo(Graph!, section, target);
        section.Edges.Clear();
        section.Edges.UnionWith(halves[0].Edges);
        section.Start = null;
        section.CustomName = null;
        Sections.Insert(Sections.IndexOf(section) + 1, new DraftSection(halves[1].Edges, NextColour(Sections.Select(s => s.Colour).ToList(), [section.Colour, .. Neighbours(section).Select(n => n.Colour)])));
        return EditOutcome.Done;
    });

    public EditOutcome Merge(IReadOnlyList<DraftSection> routes) => Edit(() =>
    {
        if (routes.Count < 2)
        {
            return new(false);
        }
        if (routes.FirstOrDefault(r => r.Locked) is { } locked)
        {
            return new(false, $"Route {Number(locked)} {Locked}");
        }
        var into = routes.OrderBy(Sections.IndexOf).First();
        foreach (var other in routes.Where(r => r != into))
        {
            into.Edges.UnionWith(other.Edges);
            Sections.Remove(other);
        }
        into.CustomName = null;
        into.Start = null;
        return EditOutcome.Done;
    });

    public EditOutcome Unassign(IReadOnlyList<DraftSection> routes) => Edit(() =>
    {
        var free = routes.Where(r => !r.Locked).ToList();
        if (free.Count == 0)
        {
            return new(false, routes.Count > 0 ? $"Route {Number(routes[0])} {Locked}" : null);
        }
        Sections.RemoveAll(free.Contains);
        return EditOutcome.Done;
    });

    public EditOutcome SetLocked(IReadOnlyList<DraftSection> routes, bool locked) => Edit(() =>
    {
        var changing = routes.Where(r => r.Locked != locked).ToList();
        changing.ForEach(r => r.Locked = locked);
        return new(changing.Count > 0);
    });

    public EditOutcome Rename(DraftSection route, string name) => Edit(() =>
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0 || trimmed == route.CustomName)
        {
            return new(false);
        }
        route.CustomName = trimmed;
        return EditOutcome.Done;
    });

    public EditOutcome MoveStart(DraftSection route, GeoPoint point) => Edit(() =>
    {
        var node = editor.NearestNode(route, point);
        if (route.Start == node)
        {
            return new(false);
        }
        route.Start = node;
        return EditOutcome.Done;
    });

    public EditOutcome Rebalance(SplitTarget target) => Edit(() =>
    {
        var before = Sections.ToDictionary(s => s, s => s.Edges.ToHashSet());
        splitter.Rebalance(Graph!, Sections, target);
        return new(Sections.Any(s => !s.Edges.SetEquals(before[s])), "Routes are as even as they can get without breaking them up.");
    });

    public bool Undo() => Step(undo, redo);

    public bool Redo() => Step(redo, undo);

    private bool Step(List<Snapshot> from, List<Snapshot> to)
    {
        if (from.Count == 0)
        {
            return false;
        }
        to.Add(Capture());
        Restore(from[^1]);
        from.RemoveAt(from.Count - 1);
        return true;
    }

    private EditOutcome Edit(Func<EditOutcome> change)
    {
        var before = Capture();
        var outcome = change();
        if (!outcome.Changed)
        {
            Restore(before);
            return outcome;
        }
        foreach (var street in Sections.SelectMany(s => s.Edges).Where(e => net[e].Partial).Select(e => net[e].Street).Distinct().ToList())
        {
            Write(street, Timeline(street));
        }
        Sections.RemoveAll(s => s.Edges.Count == 0);
        undo.Add(before);
        if (undo.Count > MaxHistory)
        {
            undo.RemoveAt(0);
        }
        redo.Clear();
        return outcome with { Note = null };
    }

    private Snapshot Capture() => new(Sections.Select(s => new SectionState(s, [.. s.Edges], s.CustomName, s.Colour, s.Locked, s.Start)).ToList(), Graph);

    private void Restore(Snapshot snapshot)
    {
        foreach (var state in snapshot.Sections)
        {
            state.Section.Edges.Clear();
            state.Section.Edges.UnionWith(state.Edges);
            state.Section.CustomName = state.Name;
            state.Section.Colour = state.Colour;
            state.Section.Locked = state.Locked;
            state.Section.Start = state.Start;
        }
        Sections = snapshot.Sections.Select(s => s.Section).ToList();
        Graph = snapshot.Graph;
    }

    private int Letterboxes(IEnumerable<int> edges) => edges.Sum(e => count(e, 0) + count(e, 1));

    private static bool Moves(DraftSection? owner, DraftSection? into) => owner != into && owner is not { Locked: true };

    private EditOutcome Reassign(IReadOnlyList<Stretch> stretches, DraftSection? into)
    {
        var touched = Touched(stretches);
        if (touched.Where(t => Moves(t.Owner, into)).Sum(t => Letterboxes([net.PieceOf(t.Street, t.From, t.To)])) == 0)
        {
            var locked = touched.FirstOrDefault(t => t.Owner is { Locked: true } && t.Owner != into).Owner;
            return new(false, locked is not null ? $"Route {Number(locked)} {Locked}"
                : into is not null && touched.Any(t => t.Owner != into) ? "Stretches with no letterboxes stay connectors; they can't be painted."
                : null);
        }
        foreach (var stretch in Merged(stretches))
        {
            var segments = Split(Timeline(stretch.Street), stretch.From, stretch.To)
                .Select(s => s.From >= stretch.From && s.To <= stretch.To && Moves(s.Owner, into) ? s with { Owner = into } : s)
                .ToList();
            Write(stretch.Street, segments);
        }
        return EditOutcome.Done;
    }

    private List<(int Street, double From, double To, DraftSection? Owner)> Touched(IReadOnlyList<Stretch> stretches) =>
        Merged(stretches)
            .SelectMany(stretch => Split(Timeline(stretch.Street), stretch.From, stretch.To)
                .Where(s => s.From >= stretch.From && s.To <= stretch.To)
                .Select(s => (stretch.Street, s.From, s.To, s.Owner)))
            .ToList();

    private IEnumerable<Stretch> Merged(IReadOnlyList<Stretch> stretches)
    {
        foreach (var street in stretches.Where(s => Streets.Contains(s.Street)).GroupBy(s => s.Street))
        {
            var length = net.Map.Edges[street.Key].Length;
            Stretch? open = null;
            foreach (var (from, to) in street.Select(s => (StreetNetwork.Round(Math.Min(s.From, s.To)), StreetNetwork.Round(Math.Max(s.From, s.To)))).OrderBy(s => s.Item1))
            {
                if (open is { } current && from <= current.To)
                {
                    open = current with { To = Math.Max(current.To, to) };
                    continue;
                }
                if (open is { } done && (done.To - done.From) * length >= MinStretchMetres)
                {
                    yield return done;
                }
                open = new(street.Key, from, to);
            }
            if (open is { } last && (last.To - last.From) * length >= MinStretchMetres)
            {
                yield return last;
            }
        }
    }

    private List<Segment> Timeline(int street)
    {
        var owned = Sections
            .SelectMany(section => section.Edges.Where(e => net[e].Street == street).Select(e => new Segment(net[e].From, net[e].To, section)))
            .OrderBy(s => s.From);
        var timeline = new List<Segment>();
        var at = 0.0;
        foreach (var segment in owned)
        {
            if (segment.From > at)
            {
                timeline.Add(new(at, segment.From, null));
            }
            timeline.Add(segment);
            at = segment.To;
        }
        if (at < 1)
        {
            timeline.Add(new(at, 1, null));
        }
        return timeline;
    }

    private static IEnumerable<Segment> Split(IEnumerable<Segment> timeline, double from, double to)
    {
        foreach (var segment in timeline)
        {
            var cuts = new[] { segment.From, from, to, segment.To }.Where(c => c >= segment.From && c <= segment.To).Distinct().Order().ToList();
            for (var i = 0; i < cuts.Count - 1; i++)
            {
                yield return segment with { From = cuts[i], To = cuts[i + 1] };
            }
        }
    }

    private static List<Segment> Joined(IEnumerable<Segment> segments)
    {
        var joined = new List<Segment>();
        foreach (var segment in segments)
        {
            if (joined.Count > 0 && joined[^1].Owner == segment.Owner)
            {
                joined[^1] = joined[^1] with { To = segment.To };
            }
            else
            {
                joined.Add(segment);
            }
        }
        return joined;
    }

    private void Write(int street, IEnumerable<Segment> segments)
    {
        var kept = Joined(Joined(segments)
            .Select(s => s.Owner is { Locked: false } && (s.From > 0 || s.To < 1) && Letterboxes([net.PieceOf(street, s.From, s.To)]) == 0 ? s with { Owner = null } : s));
        foreach (var section in Sections)
        {
            section.Edges.RemoveWhere(e => net[e].Street == street);
        }
        foreach (var segment in kept.Where(s => s.Owner is not null))
        {
            segment.Owner!.Edges.Add(net.PieceOf(street, segment.From, segment.To));
        }
    }

    private IEnumerable<int> Unlocked(int street) =>
        Joined(Timeline(street).Select(s => s.Owner is { Locked: true } ? s : s with { Owner = null }))
            .Where(s => s.Owner is null)
            .Select(s => net.PieceOf(street, s.From, s.To));

    private int NextColour() => NextColour(Sections.Select(s => s.Colour).ToList());

    private int NextColour(List<int> used, IReadOnlyCollection<int>? avoid = null)
    {
        var options = Enumerable.Range(0, colours).Where(c => avoid?.Contains(c) != true).DefaultIfEmpty(0);
        return options.MinBy(c => (used.Count(u => u == c), c));
    }

    private IEnumerable<DraftSection> Neighbours(DraftSection section) =>
        Graph is null ? [] : AreaSplitter.Neighbours(Graph, section, Sections);
}
