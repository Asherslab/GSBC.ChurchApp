using System.Text.Json;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Client.Location;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using RoutingPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;
using RecordStretch = GSBC.ChurchApp.Features.Outreach.Contracts.Stretch;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed record WalkStart(
    string Name,
    IReadOnlyList<string> Campaigns,
    WalkMode Mode,
    OutreachRegion Region,
    RouteSummary Summary,
    string DayId = "",
    string RouteId = "",
    bool WithPartner = false);

public sealed record StepProgress(int Index, SavedStep Step, int Done, double Share);

public sealed record WalkProgress(
    IReadOnlyList<StepProgress> Steps,
    int Current,
    int LetterboxesDone,
    int Metres,
    TimeSpan Elapsed,
    RoutingPoint? You,
    double Accuracy,
    bool HasFixes)
{
    public StepProgress? Now => Current >= 0 && Current < Steps.Count ? Steps[Current] : null;

    public StepProgress? Next => Steps.Skip(Current + 1).FirstOrDefault(step => step.Step.Delivers);
}

public enum NearestBox
{
    Found,
    NoFix,
    TooFar,
}

public sealed class WalkSession(
    ILocationSession location,
    IOutbox outbox,
    ILocalPreferences preferences,
    RegionMaps regions,
    OutreachStore store,
    TimeProvider time)
{
    public const double StepDoneShare = 0.9;
    private const string StateKey = "outreach.walk";
    private const string HistoryKey = "outreach.history";
    private const string Service = "/gsbc.outreach.Outreach/";
    private const string Purpose = "outreach-walk";
    private const int HistoryKept = 200;
    private const double NearestBoxMetres = 35;
    private const string SkippedPrefix = "skipped:";
    private static readonly TimeSpan SendEvery = TimeSpan.FromSeconds(90);

    private readonly SemaphoreSlim _working = new(1, 1);
    private WalkState? _state;
    private bool _loaded;
    private WalkFollower? _follower;
    private StreetGraph? _graph;
    private OutreachRegion? _region;
    private WalkProgress? _progress;

    public event Action? Changed;

    public WalkState? Current => _state;

    public WalkProgress? Progress => _progress;

    public StreetGraph? Graph => _graph;

    public bool CanTrack => location.IsAvailable;

    public async Task<WalkState?> LoadAsync()
    {
        if (_loaded)
            return _state;
        await _working.WaitAsync();
        try
        {
            if (!_loaded)
            {
                _state = await ReadStateAsync();
                _loaded = true;
                if (_state is not null)
                    await PrepareAsync(_state);
            }
        }
        finally
        {
            _working.Release();
        }
        if (_state is { Finished: false, Paused: false, Tracking: true })
            await location.StartAsync(Purpose);
        return _state;
    }

    public async Task<LocationStart> StartAsync(WalkStart start)
    {
        await LoadAsync();
        if (_state is { Finished: false })
            throw new InvalidOperationException("A walk is already on.");
        LocationFixes before = await location.FixesAsync(long.MaxValue);
        WalkState state = new()
        {
            WalkId = Guid.NewGuid().ToString(),
            StartedAt = time.GetUtcNow().UtcDateTime,
            Campaigns = [.. start.Campaigns],
            Mode = start.Mode,
            DayId = start.DayId,
            RouteId = start.RouteId,
            Name = start.Name,
            RegionId = start.Region.Id,
            RegionVersion = start.Region.Version,
            PlannedMetres = (int)Math.Round(start.Summary.Metres),
            PlannedLetterboxes = start.Summary.Letterboxes,
            PlannedSeconds = (int)Math.Round(start.Summary.Time.TotalSeconds),
            Bundles = start.Summary.BundlesToTake,
            Steps = WalkState.StepsOf(start.Summary),
            FixFrom = before.Latest,
            FixLatest = before.Latest,
            WithPartner = start.WithPartner,
            LastSentAt = time.GetUtcNow().UtcDateTime,
        };
        LocationStart started = await location.StartAsync(Purpose);
        state.Tracking = started == LocationStart.Running;
        await _working.WaitAsync();
        try
        {
            _state = state;
            _loaded = true;
            await PrepareAsync(state);
            await SendPieceAsync(state, ended: false, force: true);
            await SaveStateAsync();
        }
        finally
        {
            _working.Release();
        }
        Changed?.Invoke();
        return started;
    }

    public async Task<bool> DrainAsync()
    {
        if (_state is not { Finished: false } state || _follower is null)
            return false;
        int before = _progress?.Current ?? -1;
        await _working.WaitAsync();
        try
        {
            LocationFixes fixes = await location.FixesAsync(state.FixLatest);
            foreach (LocationFix fix in fixes.Fixes.Where(fix => fix.Seq > state.FixLatest))
                _follower.Add(new GpsFix(fix.At, new RoutingPoint(fix.Lat, fix.Lon), fix.Accuracy));
            if (fixes.Fixes.Count > 0)
                state.FixLatest = Math.Max(state.FixLatest, fixes.Latest);
            _progress = Measure(state);
            if (time.GetUtcNow().UtcDateTime - state.LastSentAt >= SendEvery || (_progress?.Current ?? -1) != before)
                await SendPieceAsync(state, ended: false, force: false);
            await SaveStateAsync();
        }
        finally
        {
            _working.Release();
        }
        Changed?.Invoke();
        return (_progress?.Current ?? -1) != before && before >= 0;
    }

    public async Task SpokeAsync()
    {
        if (_state is null)
            return;
        _state.Conversations++;
        await SaveStateAsync();
        Changed?.Invoke();
    }

    public async Task DoorAsync(DoorOutcomeKind outcome, string firstName, string contact, string note, bool consent)
    {
        if (_state is null || _graph is null || _region is null)
            return;
        _state.Doors++;
        if (outcome is DoorOutcomeKind.Spoke or DoorOutcomeKind.FollowUp)
            _state.Conversations++;
        int? box = NearestLetterbox(out _);
        if (box is { } letterbox)
        {
            bool keeps = outcome == DoorOutcomeKind.FollowUp && consent;
            await outbox.EnqueueAsync(Service + "Door", new DoorOutcome
            {
                Id = Guid.NewGuid().ToString(),
                WalkId = _state.WalkId,
                Campaign = _state.Campaigns.FirstOrDefault() ?? OutreachCampaigns.GeneralId.ToString(),
                Address = EngineInputs.Address(_graph, _region.Id, letterbox),
                Outcome = outcome,
                Consent = keeps,
                FirstName = keeps ? OutreachRules.Clip(firstName, OutreachRules.LongestFirstName) : "",
                Contact = keeps ? OutreachRules.Clip(contact, OutreachRules.LongestContact) : "",
                Note = keeps ? OutreachRules.Clip(note, OutreachRules.LongestNote) : "",
                At = time.GetUtcNow().UtcDateTime,
            }, OutreachContractsModel.Instance);
        }
        await SaveStateAsync();
        Changed?.Invoke();
    }

    public AddressPoint? NearestAddress(out NearestBox found)
    {
        int? box = NearestLetterbox(out found);
        return box is { } letterbox && _graph is not null && _region is not null ? EngineInputs.Address(_graph, _region.Id, letterbox) : null;
    }

    public AddressPoint? AddressAt(RoutingPoint point)
    {
        if (_graph is null || _region is null)
            return null;
        Flat flat = _graph.Projection.ToFlat(point);
        return _graph.Index.Nearest(flat, NearestBoxMetres) is { } position && Closest(position) is { } box
            ? EngineInputs.Address(_graph, _region.Id, box)
            : null;
    }

    public async Task<string?> NoJunkMailAsync()
    {
        if (_state is null || NearestAddress(out _) is not { } address)
            return null;
        string id = Guid.NewGuid().ToString();
        await outbox.EnqueueAsync(Service + "NoJunkMail", new NoJunkMail { Id = id, Address = address }, OutreachContractsModel.Instance);
        _state.Marked.Add(id);
        await SaveStateAsync();
        Changed?.Invoke();
        return id;
    }

    public async Task UndoNoJunkMailAsync(string id)
    {
        if (_state is null || !_state.Marked.Remove(id))
            return;
        await outbox.EnqueueAsync(Service + "UndoNoJunkMail", new MarkRef { Id = id }, OutreachContractsModel.Instance);
        await SaveStateAsync();
        Changed?.Invoke();
    }

    public async Task SkipThisBitAsync()
    {
        if (_state is null || _follower is null || _progress?.Now is not { } now)
            return;
        foreach (SavedSide side in now.Step.Sides)
        {
            _follower.Skip(side.ToStretch());
            _state.Skipped.Add(side);
        }
        _progress = Measure(_state);
        await SaveStateAsync();
        Changed?.Invoke();
    }

    public async Task PauseAsync(bool paused)
    {
        if (_state is null)
            return;
        _state.Paused = paused;
        if (paused)
        {
            await DrainAsync();
            await location.StopAsync();
        }
        else
            _state.Tracking = await location.StartAsync(Purpose) == LocationStart.Running;
        await SaveStateAsync();
        Changed?.Invoke();
    }

    public async Task FinishAsync()
    {
        if (_state is null)
            return;
        await DrainAsync();
        await location.StopAsync();
        _state.Finished = true;
        _state.EndedAt = time.GetUtcNow().UtcDateTime;
        await SaveStateAsync();
        Changed?.Invoke();
    }

    public async Task SaveAsync(IReadOnlySet<int> walkedSteps)
    {
        if (_state is not { } state)
            return;
        await _working.WaitAsync();
        try
        {
            WalkProgress? progress = _progress ?? Measure(state);
            List<SideStretch> extra = [];
            List<SideStretch> takenBack = [];
            foreach (StepProgress step in progress?.Steps ?? [])
            {
                bool walked = step.Share >= StepDoneShare;
                bool kept = walkedSteps.Contains(step.Index);
                if (kept && !walked)
                    extra.AddRange(step.Step.Sides.Select(side => side.ToStretch()));
                else if (!kept && walked)
                    takenBack.AddRange(step.Step.Sides.Select(side => side.ToStretch()));
            }
            await SendPieceAsync(state, ended: true, force: true, extra, takenBack);
            await RememberAsync(state, progress, walkedSteps);
            await location.ClearAsync(state.FixLatest);
            _state = null;
            _follower = null;
            _progress = null;
            await preferences.SetAsync(StateKey, null);
        }
        finally
        {
            _working.Release();
        }
        Changed?.Invoke();
    }

    public async Task<IReadOnlyList<WalkHistoryEntry>> HistoryAsync()
    {
        string? json = await preferences.GetAsync(HistoryKey);
        return string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize(json, WalkJson.Default.ListWalkHistoryEntry) ?? [];
    }

    public int LetterboxesOf(IReadOnlySet<int> walkedSteps) =>
        _progress?.Steps.Where(step => walkedSteps.Contains(step.Index)).Sum(step => Math.Max(step.Done, step.Share >= StepDoneShare ? step.Step.Letterboxes : 0)) ?? 0;

    private async Task PrepareAsync(WalkState state)
    {
        OutreachNow now = await store.LoadAsync();
        _region = now.Regions.FirstOrDefault(region => region.Id == state.RegionId);
        _graph = _region is null ? null : await regions.GraphAsync(_region);
        if (_graph is null || _graph.Region.Version != state.RegionVersion && _region?.Version != state.RegionVersion)
        {
            _follower = null;
            return;
        }
        _follower = new WalkFollower(_graph);
        foreach (SavedSide skipped in state.Skipped)
            _follower.Skip(skipped.ToStretch());
        LocationFixes fixes = await location.FixesAsync(state.FixFrom);
        foreach (LocationFix fix in fixes.Fixes.Where(fix => fix.Seq > state.FixFrom))
            _follower.Add(new GpsFix(fix.At, new RoutingPoint(fix.Lat, fix.Lon), fix.Accuracy));
        state.FixLatest = Math.Max(state.FixLatest, fixes.Latest);
        _progress = Measure(state);
    }

    private WalkProgress? Measure(WalkState state)
    {
        if (_graph is null || _follower is null)
            return null;
        IReadOnlyList<SideStretch> covered = _follower.Covered;
        Dictionary<(int, Side), List<SideStretch>> byStreet = covered.GroupBy(c => (c.Street, c.Side)).ToDictionary(g => g.Key, g => g.ToList());
        List<StepProgress> steps = [];
        for (int i = 0; i < state.Steps.Count; i++)
        {
            SavedStep step = state.Steps[i];
            int total = 0, done = 0;
            foreach (SavedSide side in step.Sides)
            {
                LetterboxRange boxes = _graph.Between(side.Street, (Side)side.Side, side.From, side.To);
                byStreet.TryGetValue((side.Street, (Side)side.Side), out List<SideStretch>? walked);
                for (int box = boxes.Start; box < boxes.End; box++)
                {
                    if (!_graph.LetterboxKindOf(box).IsHome())
                        continue;
                    total++;
                    double along = _graph.LetterboxAlong(box);
                    if (walked?.Any(c => along >= c.From - 0.0005 && along <= c.To + 0.0005) == true)
                        done++;
                }
            }
            double share = step.Delivers ? total == 0 ? 1 : done / (double)total : 0;
            steps.Add(new StepProgress(i, step, done, share));
        }
        IReadOnlyList<GpsFix> fixes = _follower.Fixes;
        GpsFix? last = fixes.Count > 0 ? fixes[^1] : null;
        int current = CurrentStep(steps);
        return new WalkProgress(
            steps,
            current,
            steps.Sum(step => step.Done),
            (int)Math.Round(_follower.WalkedMetres),
            (state.EndedAt ?? time.GetUtcNow().UtcDateTime) - state.StartedAt,
            last?.Point,
            last?.Accuracy ?? 0,
            fixes.Count > 0);
    }

    private int CurrentStep(IReadOnlyList<StepProgress> steps)
    {
        StreetPosition? where = _follower?.Where();
        int lastDone = -1;
        for (int i = 0; i < steps.Count; i++)
            if (steps[i].Step.Delivers && steps[i].Share >= StepDoneShare)
                lastDone = i;
        if (where is { } position)
        {
            int onStreet = Enumerable.Range(Math.Max(0, lastDone), steps.Count - Math.Max(0, lastDone))
                .FirstOrDefault(i => steps[i].Step.Delivers && steps[i].Share < StepDoneShare
                                     && steps[i].Step.Sides.Any(side => side.Street == position.Street && side.Side == (int)position.Side), -1);
            if (onStreet >= 0)
                return onStreet;
        }
        for (int i = lastDone + 1; i < steps.Count; i++)
            if (steps[i].Step.Delivers && steps[i].Share < StepDoneShare)
                return i;
        return steps.Count;
    }

    private int? NearestLetterbox(out NearestBox found)
    {
        found = NearestBox.NoFix;
        if (_graph is null || _follower is null || _follower.Fixes.Count == 0)
            return null;
        Flat flat = _graph.Projection.ToFlat(_follower.Fixes[^1].Point);
        if (_graph.Index.Nearest(flat, NearestBoxMetres, street => _graph.Street(street).CanDeliver) is not { } position || Closest(position) is not { } box)
        {
            found = NearestBox.TooFar;
            return null;
        }
        found = NearestBox.Found;
        return box;
    }

    private int? Closest(StreetPosition position)
    {
        LetterboxRange boxes = _graph!.Letterboxes(position.Street, position.Side);
        int? best = null;
        double nearest = double.MaxValue;
        for (int box = boxes.Start; box < boxes.End; box++)
        {
            if (!_graph.LetterboxKindOf(box).IsHome())
                continue;
            double gap = Math.Abs(_graph.LetterboxAlong(box) - position.Along);
            if (gap < nearest)
            {
                nearest = gap;
                best = box;
            }
        }
        return best;
    }

    private async Task SendPieceAsync(WalkState state, bool ended, bool force, IReadOnlyList<SideStretch>? extra = null, IReadOnlyList<SideStretch>? takenBack = null)
    {
        List<RecordStretch> covered = [];
        List<RecordStretch> skipped = [];
        List<RecordStretch> uncovered = [];
        if (_graph is not null && _follower is not null)
        {
            foreach (SideStretch side in _follower.Covered.Concat(extra ?? []))
            {
                RecordStretch stretch = EngineInputs.Stretch(_graph, state.RegionId, state.RegionVersion, side);
                if (stretch.To > stretch.From && state.Sent.Add(Key(stretch)))
                    covered.Add(stretch);
            }
            foreach (SavedSide side in state.Skipped)
            {
                RecordStretch stretch = EngineInputs.Stretch(_graph, state.RegionId, state.RegionVersion, side.ToStretch());
                if (state.Sent.Add(SkippedPrefix + Key(stretch)))
                    skipped.Add(stretch);
            }
            uncovered.AddRange((takenBack ?? []).Select(side => EngineInputs.Stretch(_graph, state.RegionId, state.RegionVersion, side)));
        }
        int metres = _follower is null ? 0 : (int)Math.Round(_follower.WalkedMetres);
        bool news = covered.Count > 0 || skipped.Count > 0 || uncovered.Count > 0 || metres - state.SentMetres >= 50
                    || state.Conversations != state.SentConversations || state.Doors != state.SentDoors;
        if (!force && !ended && !news)
            return;
        WalkPiece piece = new()
        {
            WalkId = state.WalkId,
            Seq = ++state.Seq,
            Campaigns = [.. state.Campaigns],
            Mode = state.Mode,
            DayId = state.DayId,
            RouteId = state.RouteId,
            StartedAt = state.StartedAt,
            At = time.GetUtcNow().UtcDateTime,
            Metres = Math.Max(0, metres - state.SentMetres),
            Covered = covered,
            Skipped = skipped,
            Uncovered = uncovered,
            Doors = state.Doors - state.SentDoors,
            Conversations = state.Conversations - state.SentConversations,
            Ended = ended,
            WithPartner = state.WithPartner,
        };
        await outbox.EnqueueAsync(Service + "RecordWalk", piece, OutreachContractsModel.Instance);
        state.SentMetres = Math.Max(state.SentMetres, metres);
        state.SentConversations = state.Conversations;
        state.SentDoors = state.Doors;
        state.LastSentAt = piece.At;
    }

    private async Task RememberAsync(WalkState state, WalkProgress? progress, IReadOnlySet<int> walkedSteps)
    {
        List<WalkHistoryEntry> history = [.. await HistoryAsync()];
        history.Insert(0, new WalkHistoryEntry
        {
            WalkId = state.WalkId,
            StartedAt = state.StartedAt,
            EndedAt = state.EndedAt ?? time.GetUtcNow().UtcDateTime,
            Metres = progress?.Metres ?? state.SentMetres,
            Letterboxes = LetterboxesOf(walkedSteps),
            Conversations = state.Conversations,
            Doors = state.Doors,
            Mode = state.Mode,
            Campaigns = [.. state.Campaigns],
            Name = state.Name,
        });
        await preferences.SetAsync(HistoryKey, JsonSerializer.Serialize(history.Take(HistoryKept).ToList(), WalkJson.Default.ListWalkHistoryEntry));
    }

    private async Task<WalkState?> ReadStateAsync()
    {
        string? json = await preferences.GetAsync(StateKey);
        if (string.IsNullOrEmpty(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize(json, WalkJson.Default.WalkState);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task SaveStateAsync()
    {
        if (_state is not null)
            await preferences.SetAsync(StateKey, JsonSerializer.Serialize(_state, WalkJson.Default.WalkState));
    }

    private static string Key(RecordStretch stretch) => $"{stretch.Street}|{(int)stretch.Side}|{stretch.From}|{stretch.To}";
}
