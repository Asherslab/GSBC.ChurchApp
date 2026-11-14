using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GeoPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;
using RecordStretch = GSBC.ChurchApp.Features.Outreach.Contracts.Stretch;
using Stretch = GSBC.ChurchApp.Features.Outreach.Routing.Stretch;

namespace GSBC.ChurchApp.Features.Outreach.Client.Planning;

public sealed record PlannedSection(DraftSection Section, int Number, string Name, SectionSummary Summary, string? Review)
{
    public int Id => Section.Id;

    public int Colour => Section.Colour;

    public bool Locked => Section.Locked;

    public int Letterboxes => Summary.Letterboxes;

    public int Minutes => (int)Math.Round(Summary.Time.TotalMinutes);

    public double Kilometres => Summary.Plan.Metres / 1000;
}

public sealed class DayPlanner
{
    public const int Colours = 9;
    public const double DefaultPerRoute = 100;

    private readonly AreaSplitter _splitter;
    private readonly string _region;
    private readonly long _regionVersion;
    private readonly Dictionary<int, string> _routeIds = [];
    private IReadOnlyList<PlannedSection>? _sections;

    public DayPlanner(StreetGraph graph, string region, long regionVersion, RouteSettings settings, IReadOnlyList<CampaignCoverage> coverage, OutreachMarks marks,
        DateOnly today, WalkKind walk, GeoPoint meeting)
    {
        _region = region;
        _regionVersion = regionVersion;
        Meeting = meeting;
        Network = new WalkNetwork(graph);
        _splitter = new AreaSplitter(new WalkPlanner(new Openness(Network, coverage, today, settings, walk, marks)));
        Draft = RouteDraft.Empty(_splitter, [], Colours);
    }

    public WalkNetwork Network { get; }

    public Openness Open => _splitter.Planner.Open;

    public GeoPoint Meeting { get; set; }

    public RouteDraft Draft { get; private set; }

    public IReadOnlyList<GeoPoint> Area { get; private set; } = [];

    public string AreaId { get; private set; } = "";

    public AreaSelection? Selection { get; private set; }

    public PlanTarget Target { get; private set; } = PlanTarget.Letterboxes;

    public double PerRoute { get; private set; } = DefaultPerRoute;

    public bool RestedToo { get; private set; }

    public string? Note { get; private set; }

    public bool HasArea => Area.Count >= 3;

    public IReadOnlyList<PlannedSection> Sections => _sections ??= Summarise();

    public int Letterboxes => Sections.Sum(s => s.Letterboxes);

    public int NeedingReview => Sections.Count(s => s.Review is not null);

    public int RestedLetterboxes => Selection?.Rested.Sum(Open.Open) ?? 0;

    public bool Draw(IReadOnlyList<GeoPoint> area, string areaId = "")
    {
        if (area.Count < 3)
            return false;
        (Area, AreaId) = (area, areaId);
        Selection = _splitter.Select(area);
        Draft = RouteDraft.Empty(_splitter, Streets(), Colours);
        return Apply(Draft.Regenerate(Split, PerRoute, Selection.Connectors(RestedToo)));
    }

    public bool SizeRoutes(PlanTarget target, double perRoute)
    {
        (Target, PerRoute) = (target, Math.Max(1, perRoute));
        return Regenerate();
    }

    public bool IncludeRested(bool restedToo)
    {
        if (RestedToo == restedToo || Selection is null)
            return false;
        RestedToo = restedToo;
        Draft = RouteDraft.Restore(_splitter, Streets(), Colours, Selection.Connectors(RestedToo), Saved());
        return Regenerate();
    }

    public bool Regenerate() => Selection is not null && Apply(Draft.Regenerate(Split, PerRoute, Selection.Connectors(RestedToo)));

    public bool Rebalance() => Apply(Draft.Rebalance(Split));

    public bool SplitInTwo(int section) => Apply(Draft.SplitInTwo(section, Split));

    public bool Apply(DraftEdit edit)
    {
        Note = edit.Note;
        if (!edit.Changed)
            return false;
        Draft = edit.Draft;
        _sections = null;
        return true;
    }

    public bool Undo() => Step(Draft.CanUndo, Draft.Undo());

    public bool Redo() => Step(Draft.CanRedo, Draft.Redo());

    public PlannedSection? Section(int id) => Sections.FirstOrDefault(s => s.Id == id);

    public PlannedSection? OwnerOf(int street, double along) =>
        Sections.FirstOrDefault(s => s.Section.Pieces.Any(p => Network[p].Street == street && Network[p].From <= along && along <= Network[p].To));

    public void Restore(DayDraft draft)
    {
        (Target, PerRoute, RestedToo) = (draft.Target, draft.PerRoute > 0 ? draft.PerRoute : DefaultPerRoute, draft.RestedToo);
        (Area, AreaId) = (draft.Area.Select(EngineInputs.Point).ToList(), draft.AreaId);
        Selection = HasArea ? _splitter.Select(Area) : null;
        List<SavedSection> saved = draft.Routes.Select(route => new SavedSection(route.Id,
            route.Stretches.Select(stretch => EngineInputs.SideStretch(Network.Graph, stretch)).OfType<SideStretch>().Select(side => side.Stretch).ToList(),
            route.Name.Length > 0 ? route.Name : null, route.Colour, route.Locked, route.Start is { } start ? EngineInputs.Point(start) : null)).ToList();
        HashSet<int> streets = [.. Streets(), .. saved.SelectMany(s => s.Pieces).Select(p => p.Street)];
        Draft = RouteDraft.Restore(_splitter, streets, Colours, Selection?.Connectors(RestedToo) ?? [], saved);
        _routeIds.Clear();
        foreach (PlannedRoute route in draft.Routes.Where(route => route.RouteId.Length > 0))
            _routeIds[route.Id] = route.RouteId;
        _sections = null;
        Note = null;
    }

    public DayDraft Save() => new()
    {
        Area = Area.Select(EngineInputs.Point).ToList(),
        AreaId = AreaId,
        Target = Target,
        PerRoute = PerRoute,
        RestedToo = RestedToo,
        Region = _region,
        Routes = Draft.Sections.Select(section => new PlannedRoute
        {
            Id = section.Id,
            Name = section.Name ?? "",
            Colour = section.Colour,
            Locked = section.Locked,
            Start = section.Start is { } node ? EngineInputs.Point(Network.Graph.Projection.ToGeo(Network.Node(node))) : null,
            Stretches = section.Stretches(Network).Select(stretch => EngineInputs.Stretch(Network.Graph, _region, _regionVersion, new SideStretch(stretch.Street, Side.Left, stretch.From, stretch.To))).ToList(),
            RouteId = _routeIds.GetValueOrDefault(section.Id, ""),
        }).ToList(),
    };

    public IReadOnlyList<DayRoute> ToRoutes() => Sections.Select(section =>
    {
        RouteSummary summary = RouteSummary.Of(section.Summary.Plan, Open);
        return new DayRoute
        {
            Id = _routeIds.GetValueOrDefault(section.Id, ""),
            Number = section.Number,
            Name = section.Name,
            Colour = section.Colour,
            Stretches = summary.Delivered.Select(side => EngineInputs.Stretch(Network.Graph, _region, _regionVersion, side)).ToList(),
            Start = EngineInputs.Point(Network.Graph.Projection.ToGeo(Network.Node(section.Summary.Plan.Start))),
            Letterboxes = section.Letterboxes,
            Metres = (int)Math.Round(section.Summary.Plan.Metres),
            Minutes = section.Minutes,
        };
    }).ToList();

    public void Published(IReadOnlyList<DayRoute> routes)
    {
        foreach ((PlannedSection section, DayRoute route) in Sections.Zip(routes))
            _routeIds[section.Id] = route.Id;
    }

    public IReadOnlyList<Stretch> Brush(IReadOnlyList<GeoPoint> stroke) => Draft.Brush(stroke);

    private SplitTarget Split => Target switch
    {
        PlanTarget.Minutes => SplitTarget.Minutes,
        PlanTarget.Bundles => SplitTarget.Bundles,
        _ => SplitTarget.Letterboxes,
    };

    private IEnumerable<int> Streets() => Selection is null ? [] : RestedToo ? [.. Selection.Fresh, .. Selection.Rested] : Selection.Fresh;

    private List<SavedSection> Saved() => Draft.Sections.Select(s => new SavedSection(s.Id, [.. s.Stretches(Network)], s.Name, s.Colour, s.Locked,
        s.Start is { } node ? Network.Graph.Projection.ToGeo(Network.Node(node)) : null)).ToList();

    private bool Step(bool can, RouteDraft next)
    {
        if (!can)
            return false;
        Draft = next;
        _sections = null;
        Note = null;
        return true;
    }

    private List<PlannedSection> Summarise() => Draft.Summaries(Meeting).Select((summary, index) =>
        new PlannedSection(summary.Section, index + 1, summary.Name, summary, Review(summary))).ToList();

    private string? Review(SectionSummary summary) =>
        summary.InPieces ? "In pieces: its streets don't join up"
        : summary.Plan.LongestConnectorMetres > Open.Settings.MaxConnectorLegMetres ? $"{summary.Plan.LongestConnectorMetres:0} m between streets with letterboxes"
        : summary.Plan.ConnectorMetres > Open.Settings.MaxConnectorTotalMetres ? $"{summary.Plan.ConnectorMetres:0} m in all between streets"
        : summary.NeedsReview ? "Needs a look"
        : null;
}
