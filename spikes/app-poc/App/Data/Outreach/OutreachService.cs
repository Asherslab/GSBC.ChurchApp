using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Gsbc.AppPoc.Native;

namespace Gsbc.AppPoc.Data.Outreach;

public enum WalkKind { Now, Area, Route }

public sealed record OutreachDay(DateTime Start, DateTime End, string Where);

public sealed record LetterboxFlag(int Edge, int Side, GeoPoint Point);

public sealed class WalkSession(WalkKind kind, int? id, string title, WalkPlan plan, bool countsTowardDay)
{
    public WalkKind Kind { get; } = kind;
    public int? Id { get; } = id;
    public string Title { get; } = title;
    public WalkPlan Plan { get; } = plan;
    public bool CountsTowardDay { get; } = countsTowardDay;
    public int LegIndex { get; set; }
    public DateTime StartedAt { get; } = DateTime.Now;
    public bool Finished => LegIndex >= Plan.Deliveries.Count;
    public WalkLeg? Current => Finished ? null : Plan.Deliveries[LegIndex];
    public WalkLeg? Next => LegIndex + 1 < Plan.Deliveries.Count ? Plan.Deliveries[LegIndex + 1] : null;
    public IReadOnlyList<string> WalkAlongToCurrent => Plan.WalkAlongBefore(LegIndex);
    public IReadOnlyList<string> WalkAlongToNext => Plan.WalkAlongBefore(LegIndex + 1);
    public int LetterboxesDone => Plan.Deliveries.Take(LegIndex).Sum(l => l.Letterboxes);
}

public sealed class OutreachService(HttpClient http, NativeBridge native, ChurchClock clock)
{
    public const int RestDays = 7;
    private const int MonthDays = 28;
    private const string NameKey = "outreach:name";
    private const string ClaimsKey = "outreach:claims";
    private const string WalkedKey = "outreach:walked";
    private const string FlagsKey = "outreach:flags";
    private const string DayTallyKey = "outreach:day-tally";
    private const string PublishedKey = "outreach:published";
    private const string ZonesKey = "outreach:zones";
    private const string ExcludedKey = "outreach:excluded";
    private const string ReportsKey = "outreach:reports";
    private const int PieceSteps = 10000;
    private static readonly DateTime OutreachPreviewMoment = new(2026, 10, 31, 9, 5, 0);
    private static readonly OutreachDay SampleDay = new(new(2026, 10, 31, 8, 30, 0), new(2026, 10, 31, 11, 30, 0), "church");
    private const int SampleDayTally = 312;

    private static readonly Dictionary<string, string[]> SampleClaims = new()
    {
        ["route:1"] = ["Ruth", "David"],
        ["route:3"] = ["Sam", "Grace", "Tom"],
        ["route:4"] = ["Priya"],
        ["route:6"] = ["Ben", "Hannah"],
        ["route:8"] = ["Joy"],
    };

    private readonly HashSet<string> myClaims = [];
    private readonly Dictionary<int, DateOnly> walked = [];
    private readonly List<LetterboxFlag> flags = [];
    private readonly Dictionary<int, DateOnly> sampleWalked = [];
    private readonly Dictionary<(int Node, double? Minutes, double? Kilometres), WalkPlan> plans = [];
    private List<ReadyRoute>? published;
    private List<ExclusionZone> zones = [];
    private List<ExcludedAddress> excluded = [];
    private List<WalkerReport> reports = [];
    private HashSet<int> zoneEdges = [];
    private Task? loading;
    private int myDayTally;

    public event Action? Changed;

    public OutreachMap? Map { get; private set; }

    public StreetNetwork? Network { get; private set; }

    public RoutePlanner? Planner { get; private set; }

    public AreaSplitter? Splitter { get; private set; }

    public RouteEditor? Editor { get; private set; }

    public IReadOnlyList<ReadyRoute> Routes => published ?? (IReadOnlyList<ReadyRoute>?)Map?.OutreachRoutes ?? [];

    public bool HasPublishedRoutes => published is not null;

    public string? WalkerName { get; private set; }

    public bool PreviewDuringOutreach { get; private set; }

    public WalkSession? Walk { get; private set; }

    public GeoPoint? Here { get; private set; }

    public DateTime Now => PreviewDuringOutreach ? OutreachPreviewMoment : clock.Now;

    public DateOnly Today => DateOnly.FromDateTime(Now);

    public OutreachDay? Day => Now < SampleDay.End && SampleDay.Start - Now <= TimeSpan.FromDays(7) ? SampleDay : null;

    public bool DayIsOn => Day is { } day && day.Start <= Now && Now < day.End;

    public int DayTally => DayIsOn ? SampleDayTally + myDayTally : myDayTally;

    public GeoPoint Church => Map is { } map ? new(map.Church[0], map.Church[1]) : new(-27.34335, 152.96467);

    public Task EnsureLoadedAsync() => loading ??= LoadAsync();

    public IReadOnlyList<ExclusionZone> Zones => zones;

    public IReadOnlyList<ExcludedAddress> ExcludedAddresses => excluded;

    public IReadOnlyList<WalkerReport> Reports => reports;

    public bool InZone(int edge) => zoneEdges.Contains(edge);

    public int StreetOf(int edge) => Network![edge].Street;

    public int Count(int edge, int side)
    {
        var piece = Network![edge];
        if (zoneEdges.Contains(piece.Street))
        {
            return 0;
        }
        var boxes = piece.Boxes(side)
            - flags.Count(f => f.Edge == piece.Street && f.Side == side && Network.Covers(edge, f.Point))
            - excluded.Count(x => x.Edge == piece.Street && x.Side == side && Network.Covers(edge, x.Point.Point));
        return Math.Max(0, boxes);
    }

    public IReadOnlyList<Mark> SkipsOn(IEnumerable<Arc> arcs)
    {
        var sides = arcs.ToList();
        return (Map?.Skips ?? []).Where(m => sides.Any(a => On(a, m.Edge, m.Side, m.Point))).ToList();
    }

    public IReadOnlyList<ExcludedAddress> DontLetterboxOn(IEnumerable<Arc> arcs)
    {
        var sides = arcs.ToList();
        return excluded.Where(x => x.ShowWalkers && sides.Any(a => On(a, x.Edge, x.Side, x.Point.Point))).ToList();
    }

    private bool On(Arc arc, int street, int side, GeoPoint point) =>
        arc.Side == side && Network![arc.Edge].Street == street && Network.Covers(arc.Edge, point);

    public IReadOnlyList<WalkMarker> MarkersFor(WalkPlan plan)
    {
        var delivering = plan.Deliveries.SelectMany(l => l.Arcs).ToList();
        return SkipsOn(delivering).Select(m => new WalkMarker(m.Point, MarkerKind.Skip, $"Skip: {m.Label}"))
            .Concat(DontLetterboxOn(plan.Arcs).Select(x => new WalkMarker(x.Point.Point, MarkerKind.DontLetterbox, "Don't letterbox here")))
            .ToList();
    }

    public int SkipCount(WalkPlan plan) => SkipsOn(plan.Deliveries.SelectMany(l => l.Arcs)).Count;

    public bool NeedsReview(ReadyRoute route) => route.Edges.Any(e => zoneEdges.Contains(StreetOf(e)));

    public async Task AddZoneAsync(IReadOnlyList<GeoPoint> polygon, ExclusionReason reason, string note)
    {
        zones.Add(new(NewId(), polygon.Select(LatLon.From).ToList(), reason, note.Trim(), DateTime.Now));
        await SaveExclusionsAsync();
    }

    public async Task RemoveZoneAsync(ExclusionZone zone)
    {
        zones.Remove(zone);
        await SaveExclusionsAsync();
    }

    public async Task<ExcludedAddress> AddExcludedAddressAsync(GeoPoint point, ExclusionReason reason, string note, bool showWalkers)
    {
        var (edge, side) = Planner!.NearestSide(point);
        var address = new ExcludedAddress(NewId(), edge, side, LatLon.From(point), reason, note.Trim(), showWalkers, DateTime.Now);
        excluded.Add(address);
        await SaveExclusionsAsync();
        return address;
    }

    public async Task SetShowWalkersAsync(ExcludedAddress address, bool show)
    {
        excluded[excluded.IndexOf(address)] = address with { ShowWalkers = show };
        await SaveExclusionsAsync();
    }

    public async Task RemoveExcludedAddressAsync(ExcludedAddress address)
    {
        excluded.Remove(address);
        await SaveExclusionsAsync();
    }

    public async Task SendReportAsync(ReportScope scope, ReportReason reason, string note, string? photo, GeoPoint? point, int? edge, string where)
    {
        reports.Insert(0, new(NewId(), scope, reason, note.Trim(), photo, point is { } p ? LatLon.From(p) : null, edge, where, DateTime.Now, ReportStatus.SentForReview));
        await SaveReportsAsync();
    }

    public async Task ResolveReportAsync(WalkerReport report, ReportStatus status)
    {
        reports[reports.IndexOf(report)] = report with { Status = status, Photo = null };
        await SaveReportsAsync();
    }

    public bool Rested(int edge) => LastWalked(edge) is { } date && Today.DayNumber - date.DayNumber < RestDays;

    public DateOnly? LastWalked(int edge)
    {
        var mine = walked.TryGetValue(edge, out var a) ? a : (DateOnly?)null;
        var sample = sampleWalked.TryGetValue(edge, out var b) ? b : (DateOnly?)null;
        return mine > sample || sample is null ? mine : sample;
    }

    public IReadOnlyList<int> RestedNear(WalkPlan plan)
    {
        const double MarginMetres = 250;
        var points = plan.Edges.SelectMany(e => Map!.Edges[e].Line).ToList();
        if (points.Count == 0)
        {
            return [];
        }
        var latMargin = MarginMetres / 111_320;
        var lonMargin = latMargin / Math.Cos(points[0][1] * Math.PI / 180);
        var (south, north) = (points.Min(p => p[1]) - latMargin, points.Max(p => p[1]) + latMargin);
        var (west, east) = (points.Min(p => p[0]) - lonMargin, points.Max(p => p[0]) + lonMargin);
        return Enumerable.Range(0, Map!.Edges.Count)
            .Where(e => Rested(e) && Planner!.Deliverable(e, Count))
            .Where(e => Planner!.Midpoint(e) is var m && m.Lat >= south && m.Lat <= north && m.Lon >= west && m.Lon <= east)
            .ToList();
    }

    public IReadOnlyList<string> StreetNames(IEnumerable<int> edges) => edges.Select(e => Map!.Edges[e].Name).Where(n => n.Length > 0).Distinct().Order().ToList();

    public double MonthCoverage()
    {
        if (Map is null)
        {
            return 0;
        }
        var reached = Map.Edges.Select((e, i) => (e, i))
            .Where(x => LastWalked(x.i) is { } d && Today.DayNumber - d.DayNumber < MonthDays)
            .Sum(x => x.e.TotalBoxes);
        return (double)reached / Map.Letterboxes.Placed;
    }

    public IReadOnlyList<string> ClaimsFor(string key)
    {
        var others = published is not null && key.StartsWith("route:", StringComparison.Ordinal) ? [] : SampleClaims.GetValueOrDefault(key) ?? [];
        return myClaims.Contains(key) && WalkerName is { } name ? [name, .. others] : others;
    }

    public bool IsMine(string key) => myClaims.Contains(key);

    public static string RouteKey(ReadyRoute route) => $"route:{route.Id}";

    public static string AreaKey(OutreachArea area) => $"area:{area.Id}";

    public int ClaimedRoutes => Routes.Count(r => ClaimsFor(RouteKey(r)).Count > 0);

    public async Task PublishRoutesAsync(IEnumerable<(string Name, IEnumerable<int> Edges)> routes)
    {
        published = routes.Select((r, i) => new ReadyRoute(i + 1, r.Name, r.Edges.Order().ToArray())).ToList();
        myClaims.RemoveWhere(k => k.StartsWith("route:", StringComparison.Ordinal));
        await native.SetAsync(ClaimsKey, string.Join(',', myClaims));
        await native.SetAsync(PublishedKey, string.Join('\n', published.Select(r =>
            $"{r.Name.Replace('|', ' ').Replace('\n', ' ')}|{string.Join('.', r.Edges.Select(PieceKey))}")));
        Changed?.Invoke();
    }

    public async Task RestoreSampleRoutesAsync()
    {
        published = null;
        myClaims.RemoveWhere(k => k.StartsWith("route:", StringComparison.Ordinal));
        await native.SetAsync(ClaimsKey, string.Join(',', myClaims));
        await native.SetAsync(PublishedKey, "");
        Changed?.Invoke();
    }

    public DateOnly? LastDone(OutreachArea area)
    {
        var latest = area.Edges.Select(LastWalked).Max();
        return latest ?? (area.LastDoneWeeksAgo is { } weeks ? DateOnly.FromDateTime(clock.Now).AddDays(-7 * weeks) : null);
    }

    public string LastDoneLabel(OutreachArea area)
    {
        if (LastDone(area) is not { } date)
        {
            return "not done yet";
        }
        var days = Today.DayNumber - date.DayNumber;
        return days switch
        {
            < 1 => "done today",
            < 7 => "done this week",
            < 14 => "last done a week ago",
            _ => $"last done {days / 7} weeks ago",
        };
    }

    public int Staleness(OutreachArea area) => LastDone(area) is { } date ? Today.DayNumber - date.DayNumber : int.MaxValue;

    public int Letterboxes(IEnumerable<int> edges) => Planner!.Letterboxes(edges, Count);

    public IReadOnlyList<OutreachArea> DueNear(GeoPoint point, int take) =>
        Map!.Areas
            .OrderBy(a => Planner!.Distance(Planner.Midpoint(a.Edges[0]), point))
            .Take(24)
            .OrderByDescending(Staleness)
            .ThenBy(a => Planner!.Distance(Planner.Midpoint(a.Edges[0]), point))
            .Take(take)
            .ToList();

    public WalkPlan PlanNow(GeoPoint start, WalkBudget budget)
    {
        var key = (Planner!.NearestNode(start), budget.Minutes, budget.Kilometres);
        if (!plans.TryGetValue(key, out var plan))
        {
            plans[key] = plan = Planner.Plan(key.Item1, budget, Rested, Count);
        }
        return plan;
    }

    public void SetHere(GeoPoint point) => Here = point;

    public WalkPlan PlanEdges(IEnumerable<int> edges, GeoPoint start)
    {
        var set = edges.ToList();
        return Planner!.Circuit(set, Planner.NearestNode(start, set), Count);
    }

    public async Task SetNameAsync(string name)
    {
        WalkerName = name.Trim();
        await native.SetAsync(NameKey, WalkerName);
        Changed?.Invoke();
    }

    public async Task SetClaimAsync(string key, bool on)
    {
        if (on)
        {
            myClaims.Add(key);
        }
        else
        {
            myClaims.Remove(key);
        }
        await native.SetAsync(ClaimsKey, string.Join(',', myClaims));
        Changed?.Invoke();
    }

    public void SetPreviewDuringOutreach(bool on)
    {
        PreviewDuringOutreach = on;
        plans.Clear();
        Changed?.Invoke();
    }

    public void StartWalk(WalkKind kind, int? id, string title, WalkPlan plan)
    {
        Walk = new(kind, id, title, plan, DayIsOn);
        Changed?.Invoke();
    }

    public async Task CompleteLegAsync()
    {
        if (Walk?.Current is not { } leg)
        {
            return;
        }
        foreach (var arc in leg.Arcs)
        {
            walked[StreetOf(arc.Edge)] = Today;
        }
        if (Walk.CountsTowardDay)
        {
            myDayTally += leg.Letterboxes;
            await native.SetAsync(DayTallyKey, myDayTally.ToString(CultureInfo.InvariantCulture));
        }
        Walk.LegIndex++;
        plans.Clear();
        await SaveWalkedAsync();
        Changed?.Invoke();
    }

    public void StepBack()
    {
        if (Walk is { LegIndex: > 0 })
        {
            Walk.LegIndex--;
            Changed?.Invoke();
        }
    }

    public void EndWalk()
    {
        Walk = null;
        Changed?.Invoke();
    }

    public async Task FlagNoJunkMailAsync(GeoPoint? where)
    {
        if (Walk?.Current is not { } leg || leg.Arcs.Count == 0)
        {
            return;
        }
        var arc = where is { } point
            ? leg.Arcs.MinBy(a => Planner!.Distance(Planner.Midpoint(a.Edge), point))
            : leg.Arcs[0];
        flags.Add(new(StreetOf(arc.Edge), arc.Side, where ?? Planner!.Midpoint(arc.Edge)));
        plans.Clear();
        await native.SetAsync(FlagsKey, string.Join(',', flags.Select(f =>
            string.Create(CultureInfo.InvariantCulture, $"{f.Edge}:{f.Side}:{f.Point.Lat}:{f.Point.Lon}"))));
        Changed?.Invoke();
    }

    public int FlagsOn(WalkLeg leg) => flags.Count(f => leg.Arcs.Any(a => On(a, f.Edge, f.Side, f.Point)));

    private async Task LoadAsync()
    {
        Map = await http.GetFromJsonAsync("data/outreach.json", OutreachMapJson.Default.OutreachMap);
        Network = new(Map!);
        Planner = new(Network);
        Splitter = new(Map!, Planner, Count);
        Editor = new(Planner);
        SeedSampleCoverage();
        LoadPublished(await native.GetAsync(PublishedKey));
        zones = Read(await native.GetAsync(ZonesKey), ExclusionsJson.Default.ListExclusionZone);
        excluded = Read(await native.GetAsync(ExcludedKey), ExclusionsJson.Default.ListExcludedAddress);
        reports = Read(await native.GetAsync(ReportsKey), ExclusionsJson.Default.ListWalkerReport);
        RefreshZoneEdges();
        WalkerName = await native.GetAsync(NameKey);
        myClaims.UnionWith((await native.GetAsync(ClaimsKey))?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? []);
        foreach (var entry in (await native.GetAsync(WalkedKey))?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [])
        {
            var parts = entry.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var edge) && DateOnly.TryParseExact(parts[1], "yyyyMMdd", out var date))
            {
                walked[edge] = date;
            }
        }
        foreach (var entry in (await native.GetAsync(FlagsKey))?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [])
        {
            var parts = entry.Split(':');
            if (parts.Length == 4 && int.TryParse(parts[0], out var edge) && int.TryParse(parts[1], out var side)
                && double.TryParse(parts[2], CultureInfo.InvariantCulture, out var lat) && double.TryParse(parts[3], CultureInfo.InvariantCulture, out var lon))
            {
                flags.Add(new(edge, side, new(lat, lon)));
            }
        }
        myDayTally = int.TryParse(await native.GetAsync(DayTallyKey), out var tally) ? tally : 0;
        Changed?.Invoke();
    }

    private static List<T> Read<T>(string? saved, JsonTypeInfo<List<T>> type)
    {
        if (string.IsNullOrEmpty(saved))
        {
            return [];
        }
        try
        {
            return JsonSerializer.Deserialize(saved, type) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string NewId() => Guid.NewGuid().ToString("N")[..10];

    private void RefreshZoneEdges()
    {
        if (Map is null)
        {
            return;
        }
        zoneEdges = zones.Count == 0
            ? []
            : Enumerable.Range(0, Map.Edges.Count)
                .Where(e => zones.Any(z => AreaSplitter.Inside(z.Polygon.Select(p => p.Point).ToList(), Planner!.Midpoint(e))))
                .ToHashSet();
    }

    private async Task SaveExclusionsAsync()
    {
        RefreshZoneEdges();
        plans.Clear();
        await native.SetAsync(ZonesKey, JsonSerializer.Serialize(zones, ExclusionsJson.Default.ListExclusionZone));
        await native.SetAsync(ExcludedKey, JsonSerializer.Serialize(excluded, ExclusionsJson.Default.ListExcludedAddress));
        Changed?.Invoke();
    }

    private async Task SaveReportsAsync()
    {
        await native.SetAsync(ReportsKey, JsonSerializer.Serialize(reports, ExclusionsJson.Default.ListWalkerReport));
        Changed?.Invoke();
    }

    private void LoadPublished(string? saved)
    {
        if (string.IsNullOrEmpty(saved))
        {
            return;
        }
        var routes = new List<ReadyRoute>();
        foreach (var line in saved.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            var edges = parts.Length == 2 ? parts[1].Split('.', StringSplitOptions.RemoveEmptyEntries).Select(ParsePiece).ToArray() : [];
            if (edges.Length > 0 && edges.All(e => e >= 0))
            {
                routes.Add(new(routes.Count + 1, parts[0], edges));
            }
        }
        published = routes.Count > 0 ? routes : null;
    }

    private string PieceKey(int edge) => Network![edge] is { Partial: true } piece
        ? string.Create(CultureInfo.InvariantCulture, $"{piece.Street}:{Math.Round(piece.From * PieceSteps)}:{Math.Round(piece.To * PieceSteps)}")
        : edge.ToString(CultureInfo.InvariantCulture);

    private int ParsePiece(string key)
    {
        var parts = key.Split(':');
        if (!int.TryParse(parts[0], CultureInfo.InvariantCulture, out var street) || street < 0 || street >= Map!.Edges.Count)
        {
            return -1;
        }
        return parts.Length == 3
            && int.TryParse(parts[1], CultureInfo.InvariantCulture, out var from)
            && int.TryParse(parts[2], CultureInfo.InvariantCulture, out var to)
            && from >= 0 && from < to && to <= PieceSteps
                ? Network!.PieceOf(street, from / (double)PieceSteps, to / (double)PieceSteps)
                : parts.Length == 1 ? street : -1;
    }

    private void SeedSampleCoverage()
    {
        var today = DateOnly.FromDateTime(clock.Now);
        foreach (var area in Map!.Areas.Where(a => a.LastDoneWeeksAgo is { } w && w <= 4))
        {
            foreach (var edge in area.Edges)
            {
                sampleWalked[edge] = today.AddDays(-7 * area.LastDoneWeeksAgo!.Value);
            }
        }
        foreach (var area in Map.Areas.OrderBy(a => Planner!.Distance(Planner.Midpoint(a.Edges[0]), Church)).Skip(1).Take(2))
        {
            foreach (var edge in area.Edges)
            {
                sampleWalked[edge] = today.AddDays(-3);
            }
        }
    }

    private Task SaveWalkedAsync() =>
        native.SetAsync(WalkedKey, string.Join(',', walked.Select(w => $"{w.Key}:{w.Value:yyyyMMdd}"))).AsTask();
}
