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

    public int Minutes => (int)Math.Round(Summary.Plan.Time.TotalMinutes);

    public double Kilometres => Summary.Plan.Metres / 1000;

    public int Area => Section.Area;
}

public sealed record AreaOverlap(DraftArea Area, PlannedSection KeptBy, IReadOnlyList<int> Pieces);

public sealed class DayPlanner
{
    private const int Colours = 9;
    private const double DefaultPerRoute = 100;

    private readonly AreaSplitter _splitter;
    private readonly string _region;
    private readonly long _regionVersion;
    private readonly Dictionary<int, string> _routeIds = [];
    private readonly Dictionary<DraftArea, AreaSelection> _selections = [];
    private IReadOnlyList<PlannedSection>? _sections;

    public DayPlanner(StreetGraph graph, string region, long regionVersion, RouteSettings settings, IReadOnlyList<CampaignCoverage> coverage, OutreachMarks marks,
        DateOnly today, WalkKind walk, GeoPoint meeting)
    {
        _region = region;
        _regionVersion = regionVersion;
        Meeting = meeting;
        Network = new WalkNetwork(graph);
        _splitter = new AreaSplitter(new WalkPlanner(new Openness(Network, coverage, today, settings, walk, marks)));
        Draft = RouteDraft.Empty(_splitter, Colours);
    }

    public WalkNetwork Network { get; }

    public Openness Open => _splitter.Planner.Open;

    public GeoPoint Meeting { get; set; }

    public RouteDraft Draft { get; private set; }

    public PlanTarget Target { get; private set; } = PlanTarget.Letterboxes;

    public double PerRoute { get; private set; } = DefaultPerRoute;

    public bool RestedToo { get; private set; }

    public string? Note { get; private set; }

    public IReadOnlyList<DraftArea> Areas => Draft.Areas;

    public bool HasSplitArea => Draft.Areas.Any(area => !area.OneRoute);

    public IReadOnlyList<PlannedSection> Sections => _sections ??= Summarise();

    public int Letterboxes => Sections.Sum(s => s.Letterboxes);

    public int NeedingReview => Sections.Count(s => s.Review is not null);

    public int RestedLetterboxes => Draft.Areas.SelectMany(area => SelectionOf(area).Rested).Distinct().Sum(Open.Open);

    public IEnumerable<int> LeftOutAsRested => RestedToo ? [] : Draft.Areas.SelectMany(area => SelectionOf(area).Rested).Distinct();

    public IReadOnlyList<AreaOverlap> Overlaps
    {
        get
        {
            Dictionary<int, PlannedSection> owners = Sections.SelectMany(s => s.Section.Pieces.Select(p => (p, s))).ToDictionary(o => o.p, o => o.s);
            return Draft.Areas
                .SelectMany(area => area.Streets.SelectMany(street => owners.Where(o => Network[o.Key].Street == street && o.Value.Area != area.Id && Draft.Area(o.Value.Area) is not null))
                    .GroupBy(o => o.Value.Id)
                    .Select(kept => new AreaOverlap(area, kept.First().Value, kept.Select(o => o.Key).ToList())))
                .ToList();
        }
    }

    public bool AddArea(IReadOnlyList<GeoPoint> outline, bool oneRoute, string savedId = "") =>
        outline.Count >= 3 && Apply(Draft.AddArea(AreaOf(Draft.NextAreaId, outline, oneRoute, savedId), Split, PerRoute));

    public bool RedrawArea(int id, IReadOnlyList<GeoPoint> outline) =>
        outline.Count >= 3 && Draft.Area(id) is { } area && Apply(Draft.ReplaceArea(AreaOf(id, outline, area.OneRoute, ""), Split, PerRoute));

    public bool SetOneRoute(int id, bool oneRoute) =>
        Draft.Area(id) is { } area && area.OneRoute != oneRoute && Apply(Draft.ReplaceArea(area with { OneRoute = oneRoute }, Split, PerRoute));

    public bool RemoveArea(int id) => Apply(Draft.RemoveArea(id));

    public bool SavedAs(int id, string savedId) => Apply(Draft.SavedAs(id, savedId));

    public bool MadeByHand(PlannedSection section) => Draft.MadeByHand(section.Section);

    public int RoutesIn(DraftArea area) => Sections.Count(s => s.Area == area.Id);

    public int LetterboxesIn(DraftArea area) => Sections.Where(s => s.Area == area.Id).Sum(s => s.Letterboxes);

    public bool SizeRoutes(PlanTarget target, double perRoute)
    {
        (Target, PerRoute) = (target, Math.Max(1, perRoute));
        return Regenerate();
    }

    public bool IncludeRested(bool restedToo)
    {
        if (RestedToo == restedToo || Draft.Areas.Count == 0)
            return false;
        RestedToo = restedToo;
        Draft = RouteDraft.Restore(_splitter, Colours, Draft.Areas.Select(area => AreaOf(area.Id, area.Outline, area.OneRoute, area.SavedId)).ToList(), Saved());
        foreach (DraftArea drawn in Draft.Areas.Where(area => area.OneRoute).ToList())
            Apply(Draft.ReplaceArea(drawn, Split, PerRoute));
        if (HasSplitArea)
            Regenerate();
        _sections = null;
        return true;
    }

    public bool Regenerate() => HasSplitArea && Apply(Draft.Regenerate(Split, PerRoute));

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
        draft = draft.Upgraded();
        (Target, PerRoute, RestedToo) = (draft.Target, draft.PerRoute > 0 ? draft.PerRoute : DefaultPerRoute, draft.RestedToo);
        List<DraftArea> areas = draft.Areas.Where(area => area.Outline.Count >= 3)
            .Select(area => AreaOf(area.Id, area.Outline.Select(EngineInputs.Point).ToList(), area.OneRoute, area.SavedAreaId)).ToList();
        List<SavedSection> saved = draft.Routes.Select(route => new SavedSection(route.Id,
            route.Stretches.Select(stretch => EngineInputs.SideStretch(Network.Graph, stretch)).OfType<SideStretch>().Select(side => side.Stretch).ToList(),
            route.Name.Length > 0 ? route.Name : null, route.Colour, route.Locked, route.Start is { } start ? EngineInputs.Point(start) : null, route.Area)).ToList();
        Draft = RouteDraft.Restore(_splitter, Colours, areas, saved);
        _routeIds.Clear();
        foreach (PlannedRoute route in draft.Routes.Where(route => route.RouteId.Length > 0))
            _routeIds[route.Id] = route.RouteId;
        _sections = null;
        Note = null;
    }

    public static DayDraft DraftOf(IReadOnlyList<DayRoute> routes) => new()
    {
        PerRoute = DefaultPerRoute,
        Routes = routes.Select((route, index) => new PlannedRoute
        {
            Id = index + 1,
            Name = route.Name,
            Colour = route.Colour,
            Start = route.Start,
            RouteId = route.Id,
            Stretches = route.Stretches.GroupBy(stretch => (stretch.Street, stretch.From, stretch.To)).Select(same => same.First())
                .Select(stretch => new RecordStretch
                {
                    Region = stretch.Region, RegionVersion = stretch.RegionVersion, Street = stretch.Street, Side = StreetSide.Left, From = stretch.From, To = stretch.To,
                    Middle = stretch.Middle, Metres = stretch.Metres, Letterboxes = stretch.Letterboxes,
                }).ToList(),
        }).ToList(),
    };

    public DayDraft Save() => new()
    {
        Areas = Draft.Areas.Select(area => new PlanArea
        {
            Id = area.Id, Outline = area.Outline.Select(EngineInputs.Point).ToList(), SavedAreaId = area.SavedId, OneRoute = area.OneRoute,
        }).ToList(),
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
            Area = section.Area,
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

    private DraftArea AreaOf(int id, IReadOnlyList<GeoPoint> outline, bool oneRoute, string savedId)
    {
        AreaSelection selection = _splitter.Select(outline);
        DraftArea area = new(id, oneRoute, RestedToo ? [.. selection.Fresh, .. selection.Rested] : selection.Fresh, [.. selection.Connectors(RestedToo)], outline, savedId);
        _selections[area] = selection;
        return area;
    }

    private AreaSelection SelectionOf(DraftArea area)
    {
        if (!_selections.TryGetValue(area, out AreaSelection? selection))
            _selections[area] = selection = _splitter.Select(area.Outline);
        return selection;
    }

    private List<SavedSection> Saved() => Draft.Sections.Select(s => new SavedSection(s.Id, [.. s.Stretches(Network)], s.Name, s.Colour, s.Locked,
        s.Start is { } node ? Network.Graph.Projection.ToGeo(Network.Node(node)) : null, s.Area)).ToList();

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
