namespace Gsbc.AppPoc.Data.Outreach;

public sealed record EditOutcome(bool Changed, string? Note = null)
{
    public static readonly EditOutcome Done = new(true);
}

public sealed record CutPreview(DraftSection Route, HashSet<int> Kept, HashSet<int> Cut, int NewColour);

public sealed class RouteDraft(AreaSplitter splitter, RouteEditor editor, LetterboxCount count, int colours)
{
    public const int MaxHistory = 50;
    private const string Locked = "is locked. Unlock it to change its streets.";

    private sealed record SectionState(DraftSection Section, int[] Edges, string? Name, int Colour, bool Locked, int? Start);

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

    public EditOutcome Regenerate(IEnumerable<int> edges, SplitTarget target, double perRoute, IEnumerable<int> connectors) => Edit(() =>
    {
        var locked = Sections.Where(s => s.Locked).ToList();
        var taken = locked.SelectMany(s => s.Edges).ToHashSet();
        var (fresh, graph) = splitter.Split(edges.Where(e => !taken.Contains(e)), target, perRoute, connectors);
        Sections = [.. locked];
        foreach (var section in fresh)
        {
            section.Colour = NextColour();
            Sections.Add(section);
        }
        Graph = graph;
        return EditOutcome.Done;
    });

    public EditOutcome Paint(DraftSection into, IEnumerable<int> edges) => Edit(() =>
    {
        if (into.Locked)
        {
            return new(false, $"Route {Number(into)} {Locked}");
        }
        var touched = edges.Where(e => !into.Edges.Contains(e)).ToList();
        var paintable = touched.Where(e => Streets.Contains(e) && count(e, 0) + count(e, 1) > 0).ToList();
        var movable = paintable.Where(e => OwnerOf(e) is not { Locked: true }).ToList();
        if (movable.Count == 0)
        {
            return new(false, touched.Count == 0 ? null
                : paintable.Count > 0 ? $"Route {Number(OwnerOf(paintable[0])!)} {Locked}"
                : "Streets with no letterboxes stay connectors; they can't be painted.");
        }
        foreach (var edge in movable)
        {
            OwnerOf(edge)?.Edges.Remove(edge);
            into.Edges.Add(edge);
        }
        return EditOutcome.Done;
    });

    public EditOutcome Erase(IEnumerable<int> edges) => Edit(() =>
    {
        var owned = edges.Select(e => (Edge: e, Owner: OwnerOf(e))).Where(x => x.Owner is not null).ToList();
        var free = owned.Where(x => !x.Owner!.Locked).ToList();
        if (free.Count == 0)
        {
            return new(false, owned.Count > 0 ? $"Route {Number(owned[0].Owner!)} {Locked}" : null);
        }
        foreach (var (edge, owner) in free)
        {
            owner!.Edges.Remove(edge);
        }
        return EditOutcome.Done;
    });

    public HashSet<int> Lassoed(DraftSection? into, IReadOnlyList<GeoPoint> loop, RoutePlanner planner) =>
        loop.Count < 3 ? [] : Streets
            .Where(e => count(e, 0) + count(e, 1) > 0 && into?.Edges.Contains(e) != true && OwnerOf(e) is not { Locked: true })
            .Where(e => AreaSplitter.Inside(loop, planner.Midpoint(e)))
            .ToHashSet();

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
        var used = Sections.Select(s => s.Colour).ToList();
        var previews = new List<CutPreview>();
        foreach (var cut in editor.Cut(Sections, line))
        {
            var colour = NextColour(used, [cut.Route.Colour, .. Neighbours(cut.Route).Select(n => n.Colour)]);
            used.Add(colour);
            var (kept, rest) = Letterboxes(cut.Kept) >= Letterboxes(cut.Cut) ? (cut.Kept, cut.Cut) : (cut.Cut, cut.Kept);
            previews.Add(new(cut.Route, kept, rest, colour));
        }
        return previews;
    }

    public EditOutcome Cut(IReadOnlyList<GeoPoint> line) => Edit(() =>
    {
        var previews = PreviewCut(line);
        if (previews.Count == 0)
        {
            return new(false, "That cut doesn't divide a route. Drag right across one.");
        }
        foreach (var preview in previews)
        {
            var number = Number(preview.Route);
            preview.Route.Edges.ExceptWith(preview.Cut);
            preview.Route.CustomName = $"Route {number}a";
            preview.Route.Start = null;
            Sections.Insert(Sections.IndexOf(preview.Route) + 1, new DraftSection(preview.Cut, preview.NewColour) { CustomName = $"Route {number}b" });
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

    private int NextColour() => NextColour(Sections.Select(s => s.Colour).ToList());

    private int NextColour(List<int> used, IReadOnlyCollection<int>? avoid = null)
    {
        var options = Enumerable.Range(0, colours).Where(c => avoid?.Contains(c) != true).DefaultIfEmpty(0);
        return options.MinBy(c => (used.Count(u => u == c), c));
    }

    private IEnumerable<DraftSection> Neighbours(DraftSection section) =>
        Graph is null ? [] : AreaSplitter.Neighbours(Graph, section, Sections);
}
