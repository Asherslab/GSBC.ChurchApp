using System.Collections.Immutable;

namespace GSBC.ChurchApp.Features.Outreach.Routing;

public sealed record DraftSection(int Id, ImmutableHashSet<int> Pieces, string? Name, int Colour, bool Locked, int? Start)
{
    public IEnumerable<Stretch> Stretches(WalkNetwork network) => Pieces.Select(p => network[p].Stretch).OrderBy(s => s.Street).ThenBy(s => s.From);
}

public sealed record DraftEdit(RouteDraft Draft, bool Changed, string? Note = null);

public sealed record CutPreview(int Section, ImmutableHashSet<int> Kept, IReadOnlyList<(ImmutableHashSet<int> Pieces, int Colour)> Pieces);

public sealed class RouteDraft
{
    public const int MaxHistory = 50;
    public const double SnapToCornerMetres = 15;
    private const double BrushReachMetres = 20;
    private const double MinStretchMetres = 1;
    private const string LockedNote = "is locked. Unlock it to change its streets.";

    private sealed record Snapshot(ImmutableList<DraftSection> Sections, SplitGraph? Graph, int NextId);

    private sealed class Working(DraftSection section)
    {
        public int Id { get; } = section.Id;

        public HashSet<int> Pieces { get; } = [.. section.Pieces];

        public string? Name { get; set; } = section.Name;

        public int Colour { get; set; } = section.Colour;

        public bool Locked { get; set; } = section.Locked;

        public int? Start { get; set; } = section.Start;

        public DraftSection Freeze() => new(Id, [.. Pieces], Name, Colour, Locked, Start);
    }

    private sealed record Segment(double From, double To, Working? Owner);

    private readonly AreaSplitter splitter;
    private readonly WalkNetwork network;
    private readonly int colours;
    private readonly ImmutableStack<Snapshot> undo;
    private readonly ImmutableStack<Snapshot> redo;
    private readonly int nextId;

    private RouteDraft(AreaSplitter splitter, int colours, IReadOnlySet<int> streets, Snapshot now, ImmutableStack<Snapshot> undo, ImmutableStack<Snapshot> redo)
    {
        this.splitter = splitter;
        network = splitter.Planner.Network;
        this.colours = colours;
        Streets = streets;
        Sections = now.Sections;
        Graph = now.Graph;
        nextId = now.NextId;
        this.undo = undo;
        this.redo = redo;
    }

    public static RouteDraft Empty(AreaSplitter splitter, IEnumerable<int> streets, int colours) =>
        new(splitter, colours, streets.ToHashSet(), new Snapshot([], null, 1), [], []);

    public ImmutableList<DraftSection> Sections { get; }

    public SplitGraph? Graph { get; }

    public IReadOnlySet<int> Streets { get; }

    public bool CanUndo => !undo.IsEmpty;

    public bool CanRedo => !redo.IsEmpty;

    public int Number(int section) => Sections.FindIndex(s => s.Id == section) + 1;

    public DraftSection? Section(int id) => Sections.Find(s => s.Id == id);

    public DraftSection? OwnerOf(int piece) => Sections.Find(s => s.Pieces.Contains(piece));

    public RouteDraft Undo() => undo.IsEmpty ? this : new RouteDraft(splitter, colours, Streets, undo.Peek(), undo.Pop(), redo.Push(Now));

    public RouteDraft Redo() => redo.IsEmpty ? this : new RouteDraft(splitter, colours, Streets, redo.Peek(), undo.Push(Now), redo.Pop());

    public IEnumerable<SectionSummary> Summaries(GeoPoint meeting) =>
        Graph is null ? [] : Sections.Select(s => splitter.Summarise(s, meeting, Graph));

    public DraftEdit Regenerate(SplitTarget target, double perRoute, IEnumerable<int> connectors) => Edit(state =>
    {
        List<Working> locked = state.Where(s => s.Locked).ToList();
        HashSet<int> lockedStreets = locked.SelectMany(s => s.Pieces).Select(p => network[p].Street).ToHashSet();
        List<int> free = Streets.SelectMany(s => lockedStreets.Contains(s) ? Unlocked(state, s) : [s]).ToList();
        (List<HashSet<int>> fresh, SplitGraph graph) = splitter.Split(free, target, perRoute, connectors);
        state.RemoveAll(s => !s.Locked);
        foreach (HashSet<int> pieces in fresh)
            state.Add(New(state, pieces));
        return (true, null, graph);
    });

    public int Moving(int? into, IReadOnlyList<Stretch> stretches)
    {
        List<Working> state = Thaw();
        Working? target = state.Find(s => s.Id == into);
        return Touched(state, stretches).Where(t => Moves(t.Owner, target)).Sum(t => Letterboxes(network.PieceOf(t.Street, t.From, t.To)));
    }

    public DraftEdit Paint(int into, IReadOnlyList<Stretch> stretches) => Edit(state =>
    {
        Working target = state.Single(s => s.Id == into);
        return target.Locked ? (false, $"Route {Number(into)} {LockedNote}", Graph) : Reassign(state, stretches, target);
    });

    public DraftEdit Erase(IReadOnlyList<Stretch> stretches) => Edit(state => Reassign(state, stretches, null));

    public IReadOnlySet<int> Lassoed(int? into, IReadOnlyList<GeoPoint> loop)
    {
        if (loop.Count < 3)
            return new HashSet<int>();
        Flat[] polygon = loop.Select(network.Graph.Projection.ToFlat).ToArray();
        List<Working> state = Thaw();
        Cuts cuts = network.CutsOf(state.SelectMany(s => s.Pieces));
        return Streets.SelectMany(street => network.Split(street, cuts.Of(street) ?? []))
            .Where(p => Letterboxes(p) > 0)
            .Where(p => !(OwnerOf(p) is { } owner && (owner.Id == into || owner.Locked)))
            .Where(p => Geometry.Inside(polygon, network.Midpoint(p)))
            .ToHashSet();
    }

    public DraftEdit Lasso(int? into, IReadOnlyList<GeoPoint> loop) => Edit(state =>
    {
        Working? target = state.Find(s => s.Id == into);
        if (target is { Locked: true })
            return (false, $"Route {Number(target.Id)} {LockedNote}", Graph);
        IReadOnlySet<int> captured = Lassoed(into, loop);
        if (captured.Count == 0)
            return (false, "The loop didn't take in any streets with letterboxes.", Graph);
        foreach (int piece in captured)
            state.Find(s => s.Pieces.Contains(piece))?.Pieces.Remove(piece);
        if (target is null)
            state.Add(New(state, captured));
        else
            target.Pieces.UnionWith(captured);
        return (true, null, Graph);
    });

    public IReadOnlyList<CutPreview> PreviewCut(IReadOnlyList<GeoPoint> line)
    {
        List<CutPreview> previews = [];
        if (Graph is null || line.Count < 2)
            return previews;
        List<Flat> cut = line.Select(network.Graph.Projection.ToFlat).ToList();
        List<int> used = Sections.Select(s => s.Colour).ToList();
        foreach (DraftSection section in Sections.Where(s => !s.Locked))
        {
            if (CutSection(section, cut) is not { } split)
                continue;
            List<int> avoid = [section.Colour, .. Neighbours(section).Select(n => n.Colour)];
            List<(ImmutableHashSet<int>, int)> pieces = [];
            foreach (HashSet<int> piece in split.Pieces)
            {
                int colour = NextColour(used, avoid);
                used.Add(colour);
                avoid.Add(colour);
                pieces.Add(([.. piece], colour));
            }
            previews.Add(new CutPreview(section.Id, [.. split.Kept], pieces));
        }
        return previews;
    }

    public bool CutTouches(IReadOnlyList<GeoPoint> line)
    {
        List<Flat> cut = line.Select(network.Graph.Projection.ToFlat).ToList();
        return Graph is not null && cut.Count > 1 && Sections.Any(s => !s.Locked && s.Pieces.Any(p => Crossings(cut, p).Count > 0));
    }

    public DraftEdit Cut(IReadOnlyList<GeoPoint> line) => Edit(state =>
    {
        IReadOnlyList<CutPreview> previews = PreviewCut(line);
        if (previews.Count == 0)
        {
            return (false, CutTouches(line)
                ? "The route is still joined elsewhere. Cut across every street between the two parts."
                : "That cut doesn't cross a route. Drag across its streets where the split should be.", Graph);
        }
        foreach (CutPreview preview in previews)
        {
            Working section = state.Single(s => s.Id == preview.Section);
            int number = state.IndexOf(section) + 1;
            section.Pieces.IntersectWith(preview.Kept);
            section.Name = $"Route {number}a";
            section.Start = null;
            int at = state.IndexOf(section);
            for (int i = 0; i < preview.Pieces.Count; i++)
            {
                Working piece = New(state, preview.Pieces[i].Pieces, preview.Pieces[i].Colour);
                piece.Name = $"Route {number}{(char)('b' + i)}";
                state.Insert(at + 1 + i, piece);
            }
        }
        return (true, null, Graph);
    });

    public DraftEdit SplitInTwo(int id, SplitTarget target) => Edit(state =>
    {
        Working section = state.Single(s => s.Id == id);
        if (section.Locked || section.Pieces.Count < 2 || Graph is null)
            return (false, section.Locked ? $"Route {Number(id)} {LockedNote}" : null, Graph);
        List<HashSet<int>> halves = splitter.SplitInTwo(Graph, section.Pieces, target);
        section.Pieces.Clear();
        section.Pieces.UnionWith(halves[0]);
        section.Start = null;
        section.Name = null;
        DraftSection frozen = section.Freeze();
        Working other = New(state, halves[1], NextColour(state.Select(s => s.Colour).ToList(), [section.Colour, .. Neighbours(frozen).Select(n => n.Colour)]));
        state.Insert(state.IndexOf(section) + 1, other);
        return (true, null, Graph);
    });

    public DraftEdit Merge(IReadOnlyList<int> ids) => Edit(state =>
    {
        List<Working> routes = state.Where(s => ids.Contains(s.Id)).ToList();
        if (routes.Count < 2)
            return (false, null, Graph);
        if (routes.Find(r => r.Locked) is { } locked)
            return (false, $"Route {Number(locked.Id)} {LockedNote}", Graph);
        Working into = routes[0];
        foreach (Working other in routes.Skip(1))
        {
            into.Pieces.UnionWith(other.Pieces);
            state.Remove(other);
        }
        into.Name = null;
        into.Start = null;
        return (true, null, Graph);
    });

    public DraftEdit Unassign(IReadOnlyList<int> ids) => Edit(state =>
    {
        int removed = state.RemoveAll(s => ids.Contains(s.Id) && !s.Locked);
        return (removed > 0, removed == 0 && ids.Count > 0 ? $"Route {Number(ids[0])} {LockedNote}" : null, Graph);
    });

    public DraftEdit SetLocked(IReadOnlyList<int> ids, bool locked) => Edit(state =>
    {
        List<Working> changing = state.Where(s => ids.Contains(s.Id) && s.Locked != locked).ToList();
        changing.ForEach(s => s.Locked = locked);
        return (changing.Count > 0, null, Graph);
    });

    public DraftEdit Rename(int id, string name) => Edit(state =>
    {
        Working section = state.Single(s => s.Id == id);
        string trimmed = name.Trim();
        if (trimmed.Length == 0 || trimmed == section.Name)
            return (false, null, Graph);
        section.Name = trimmed;
        return (true, null, Graph);
    });

    public DraftEdit MoveStart(int id, GeoPoint point) => Edit(state =>
    {
        Working section = state.Single(s => s.Id == id);
        int node = splitter.Planner.NearestEnd(section.Pieces, network.Graph.Projection.ToFlat(point));
        if (section.Start == node)
            return (false, null, Graph);
        section.Start = node;
        return (true, null, Graph);
    });

    public DraftEdit Rebalance(SplitTarget target) => Edit(state =>
    {
        if (Graph is null)
            return (false, null, Graph);
        List<Working> free = state.Where(s => !s.Locked).ToList();
        List<HashSet<int>> sections = free.Select(s => new HashSet<int>(s.Pieces)).ToList();
        splitter.Rebalance(Graph, sections, target);
        bool changed = false;
        for (int i = 0; i < free.Count; i++)
        {
            changed |= !free[i].Pieces.SetEquals(sections[i]);
            free[i].Pieces.Clear();
            free[i].Pieces.UnionWith(sections[i]);
        }
        return (changed, changed ? null : "Routes are as even as they can get without breaking them up.", Graph);
    });

    public IReadOnlyList<Stretch> Brush(IReadOnlyList<GeoPoint> stroke)
    {
        StreetGraph graph = network.Graph;
        List<(int Street, double From, double To)> runs = [];
        foreach (GeoPoint point in stroke)
        {
            int? previous = runs.Count > 0 ? runs[^1].Street : null;
            Flat flat = graph.Projection.ToFlat(point);
            StreetPosition? near = graph.Index.Nearest(flat, BrushReachMetres, Streets.Contains);
            if (near is not { } position)
                continue;
            if (previous is { } street && street != position.Street && graph.Project(street, flat).Distance <= position.Distance + 3)
                position = graph.Project(street, flat);
            if (runs.Count > 0 && runs[^1].Street == position.Street)
                runs[^1] = (position.Street, Math.Min(runs[^1].From, position.Along), Math.Max(runs[^1].To, position.Along));
            else
                runs.Add((position.Street, position.Along, position.Along));
        }
        if (stroke.Count == 1 && runs.Count == 1)
            return [Stretch.Whole(runs[0].Street)];
        return runs.Select(r => Snap(new Stretch(r.Street, r.From, r.To))).Where(s => s.Length(graph) >= MinStretchMetres).ToList();
    }

    public Stretch Snap(Stretch stretch)
    {
        double length = network.Graph.Street(stretch.Street).Length;
        double from = stretch.From * length <= SnapToCornerMetres ? 0 : stretch.From;
        double to = (1 - stretch.To) * length <= SnapToCornerMetres ? 1 : stretch.To;
        return stretch with { From = from, To = to };
    }

    private Snapshot Now => new(Sections, Graph, nextId);

    private List<Working> Thaw() => Sections.Select(s => new Working(s)).ToList();

    private DraftEdit Edit(Func<List<Working>, (bool Changed, string? Note, SplitGraph? Graph)> change)
    {
        List<Working> state = Thaw();
        (bool changed, string? note, SplitGraph? graph) = change(state);
        if (!changed)
            return new DraftEdit(this, false, note);
        foreach (int street in state.SelectMany(s => s.Pieces).Where(p => network[p].Partial).Select(p => network[p].Street).Distinct().ToList())
            Write(state, street, Timeline(state, street));
        state.RemoveAll(s => s.Pieces.Count == 0);
        ImmutableStack<Snapshot> history = undo.Push(Now);
        if (history.Count() > MaxHistory)
            history = ImmutableStack.CreateRange(history.Take(MaxHistory).Reverse());
        Snapshot next = new([.. state.Select(s => s.Freeze())], graph, NextId(state));
        return new DraftEdit(new RouteDraft(splitter, colours, Streets, next, history, []), true);
    }

    private int NextId(List<Working> state) => Math.Max(nextId, state.Count == 0 ? 0 : state.Max(s => s.Id) + 1);

    private Working New(List<Working> state, IEnumerable<int> pieces, int? colour = null)
    {
        return new Working(new DraftSection(NextId(state), [.. pieces], null, colour ?? NextColour(state.Select(s => s.Colour).ToList()), false, null));
    }

    private int Letterboxes(int piece) => splitter.Planner.Open.Open(piece);

    private static bool Moves(Working? owner, Working? into) => owner != into && owner is not { Locked: true };

    private (bool, string?, SplitGraph?) Reassign(List<Working> state, IReadOnlyList<Stretch> stretches, Working? into)
    {
        var touched = Touched(state, stretches);
        if (touched.Where(t => Moves(t.Owner, into)).Sum(t => Letterboxes(network.PieceOf(t.Street, t.From, t.To))) == 0)
        {
            Working? locked = touched.FirstOrDefault(t => t.Owner is { Locked: true } && t.Owner != into).Owner;
            return (false, locked is not null ? $"Route {Number(locked.Id)} {LockedNote}"
                : into is not null && touched.Any(t => t.Owner != into) ? "Stretches with no letterboxes stay connectors; they can't be painted."
                : null, Graph);
        }
        foreach (Stretch stretch in Merged(stretches))
        {
            List<Segment> segments = Split(Timeline(state, stretch.Street), stretch.From, stretch.To)
                .Select(s => s.From >= stretch.From && s.To <= stretch.To && Moves(s.Owner, into) ? s with { Owner = into } : s)
                .ToList();
            Write(state, stretch.Street, segments);
        }
        return (true, null, Graph);
    }

    private List<(int Street, double From, double To, Working? Owner)> Touched(List<Working> state, IReadOnlyList<Stretch> stretches) =>
        Merged(stretches)
            .SelectMany(stretch => Split(Timeline(state, stretch.Street), stretch.From, stretch.To)
                .Where(s => s.From >= stretch.From && s.To <= stretch.To)
                .Select(s => (stretch.Street, s.From, s.To, s.Owner)))
            .ToList();

    private IEnumerable<Stretch> Merged(IReadOnlyList<Stretch> stretches)
    {
        foreach (IGrouping<int, Stretch> street in stretches.Where(s => Streets.Contains(s.Street)).GroupBy(s => s.Street))
        {
            double length = network.Graph.Street(street.Key).Length;
            Stretch? pending = null;
            foreach ((double from, double to) in street.Select(s => (WalkNetwork.Round(Math.Min(s.From, s.To)), WalkNetwork.Round(Math.Max(s.From, s.To)))).OrderBy(s => s.Item1))
            {
                if (pending is { } current && from <= current.To)
                {
                    pending = current with { To = Math.Max(current.To, to) };
                    continue;
                }
                if (pending is { } done && (done.To - done.From) * length >= MinStretchMetres)
                    yield return done;
                pending = new Stretch(street.Key, from, to);
            }
            if (pending is { } last && (last.To - last.From) * length >= MinStretchMetres)
                yield return last;
        }
    }

    private List<Segment> Timeline(List<Working> state, int street)
    {
        IEnumerable<Segment> owned = state
            .SelectMany(section => section.Pieces.Where(p => network[p].Street == street).Select(p => new Segment(network[p].From, network[p].To, section)))
            .OrderBy(s => s.From);
        List<Segment> timeline = [];
        double at = 0;
        foreach (Segment segment in owned)
        {
            if (segment.From > at)
                timeline.Add(new Segment(at, segment.From, null));
            timeline.Add(segment);
            at = segment.To;
        }
        if (at < 1)
            timeline.Add(new Segment(at, 1, null));
        return timeline;
    }

    private static IEnumerable<Segment> Split(IEnumerable<Segment> timeline, double from, double to)
    {
        foreach (Segment segment in timeline)
        {
            List<double> cuts = new[] { segment.From, from, to, segment.To }.Where(c => c >= segment.From && c <= segment.To).Distinct().Order().ToList();
            for (int i = 0; i < cuts.Count - 1; i++)
                yield return segment with { From = cuts[i], To = cuts[i + 1] };
        }
    }

    private static List<Segment> Joined(IEnumerable<Segment> segments)
    {
        List<Segment> joined = [];
        foreach (Segment segment in segments)
        {
            if (joined.Count > 0 && joined[^1].Owner == segment.Owner)
                joined[^1] = joined[^1] with { To = segment.To };
            else
                joined.Add(segment);
        }
        return joined;
    }

    private void Write(List<Working> state, int street, IEnumerable<Segment> segments)
    {
        List<Segment> kept = Joined(Joined(segments)
            .Select(s => s.Owner is { Locked: false } && (s.From > 0 || s.To < 1) && Letterboxes(network.PieceOf(street, s.From, s.To)) == 0 ? s with { Owner = null } : s));
        foreach (Working section in state)
            section.Pieces.RemoveWhere(p => network[p].Street == street);
        foreach (Segment segment in kept.Where(s => s.Owner is not null))
            segment.Owner!.Pieces.Add(network.PieceOf(street, segment.From, segment.To));
    }

    private IEnumerable<int> Unlocked(List<Working> state, int street) =>
        Joined(Timeline(state, street).Select(s => s.Owner is { Locked: true } ? s : s with { Owner = null }))
            .Where(s => s.Owner is null)
            .Select(s => network.PieceOf(street, s.From, s.To));

    private int NextColour(List<int> used, IReadOnlyCollection<int>? avoid = null)
    {
        IEnumerable<int> options = Enumerable.Range(0, colours).Where(c => avoid?.Contains(c) != true).DefaultIfEmpty(0);
        return options.MinBy(c => (used.Count(u => u == c), c));
    }

    private IEnumerable<DraftSection> Neighbours(DraftSection section) =>
        Graph is null ? [] : Sections.Where(s => s.Id != section.Id && s.Pieces.Any(p => section.Pieces.Any(q => Graph.Touch(p, q))));

    private (HashSet<int> Kept, List<HashSet<int>> Pieces)? CutSection(DraftSection section, List<Flat> cut)
    {
        SplitGraph graph = Graph!;
        List<(int Piece, List<double> Along)> crossed = section.Pieces.Select(p => (Piece: p, Along: Crossings(cut, p))).Where(c => c.Along.Count > 0).ToList();
        if (crossed.Count == 0)
            return null;
        HashSet<int> severed = crossed.Select(c => c.Piece).ToHashSet();
        List<HashSet<int>> pieces = [];
        HashSet<int> untouched = [];
        foreach (List<int> whole in graph.Components(section.Pieces))
        {
            List<HashSet<int>> parts = graph.Components(whole.Where(p => !severed.Contains(p)).ToList()).Select(p => p.ToHashSet()).ToList();
            if (whole.Any(severed.Contains) && parts.Count > 1)
                pieces.AddRange(parts);
            else
                untouched.UnionWith(whole);
        }
        if (pieces.Count == 0)
            return null;
        foreach ((int piece, List<double> along) in crossed.Where(c => !untouched.Contains(c.Piece)))
        {
            (int near, int far) = along.Min() >= 1 - along.Max() ? (network[piece].U, network[piece].V) : (network[piece].V, network[piece].U);
            HashSet<int> home = pieces.FirstOrDefault(p => p.Any(q => graph.Reaches(q, near)))
                ?? pieces.FirstOrDefault(p => p.Any(q => graph.Reaches(q, far)))
                ?? pieces.MaxBy(p => p.Sum(Letterboxes))!;
            home.Add(piece);
        }
        HashSet<int> kept = pieces.MaxBy(p => p.Sum(Letterboxes))!;
        kept.UnionWith(untouched);
        return (kept, pieces.Where(p => p != kept && p.Count > 0).ToList());
    }

    private List<double> Crossings(List<Flat> cut, int piece)
    {
        IReadOnlyList<Flat> line = network.FlatLine(piece);
        double[] lengths = new double[line.Count - 1];
        for (int i = 0; i < lengths.Length; i++)
            lengths[i] = Flat.Distance(line[i], line[i + 1]);
        double total = Math.Max(lengths.Sum(), 1e-9), walked = 0;
        List<double> crossings = [];
        for (int i = 0; i < lengths.Length; i++)
        {
            for (int j = 0; j < cut.Count - 1; j++)
            {
                if (Geometry.Crossing(line[i], line[i + 1], cut[j], cut[j + 1]) is { } t)
                    crossings.Add((walked + t * lengths[i]) / total);
            }
            walked += lengths[i];
        }
        return crossings;
    }
}
