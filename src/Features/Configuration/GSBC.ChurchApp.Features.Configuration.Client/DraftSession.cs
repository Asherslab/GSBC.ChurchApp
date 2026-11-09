using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Client;

public readonly record struct AreaKey(string Area, string? ScopeId);

internal sealed class DraftSession(Draft draft, SurfaceCatalog surfaces, TimeProvider? time = null)
{
    private const int MostSteps = 200;
    private static readonly TimeSpan OneGesture = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan Typing = TimeSpan.FromSeconds(1.5);

    private readonly Dictionary<AreaKey, IReadOnlyList<StoredPlacement>> _live = [];
    private readonly Dictionary<string, PlacementPatch> _patches = draft.Patches().ToDictionary(patch => patch.PlacementId);
    private readonly HashSet<string> _dirty = [];
    private readonly List<Step> _back = [];
    private readonly List<Step> _forward = [];
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public Draft Draft { get; private set; } = draft;

    public bool HasUnsaved => _dirty.Count > 0;

    public int Edits { get; private set; }

    public bool CanStepBack => _back.Count > 0;

    public bool CanStepForward => _forward.Count > 0;

    public IReadOnlyCollection<PlacementPatch> Patches => _patches.Values;

    public IEnumerable<AreaKey> TouchedAreas => _patches.Values.Select(patch => new AreaKey(patch.Area, patch.ScopeId)).Distinct();

    public bool Knows(AreaKey area) => _live.ContainsKey(area);

    public void UseLive(AreaKey area, IEnumerable<StoredPlacement> live) => _live[area] = live.ToList();

    public int CountIn(AreaKey area) => _patches.Values.Count(patch => patch.Touches(area.Area, area.ScopeId));

    public IReadOnlyList<ArrangedSurface> Arranged(SurfaceArea area, string? scopeId)
    {
        AreaKey key = new(area.Key, scopeId);
        IEnumerable<StoredPlacement> composed = DraftComposition.Apply(_live.GetValueOrDefault(key, []), InArea(key));
        return SurfaceLayout.Arrange(surfaces.In(area), composed);
    }

    public IReadOnlyList<ArrangedSurface> Everyone(SurfaceArea area, string? scopeId) =>
        SurfaceLayout.Arrange(surfaces.In(area), _live.GetValueOrDefault(new AreaKey(area.Key, scopeId), []));

    public PlacementPatch? PatchFor(string? placementId) => placementId is null ? null : _patches.GetValueOrDefault(placementId);

    public IReadOnlyList<ChangedForEveryone> ChangedForEveryone =>
        _live.SelectMany(pair => DraftComposition.ChangedSince(pair.Value, InArea(pair.Key))).ToList();

    public ChangedForEveryone? ChangedFor(string? placementId) =>
        ChangedForEveryone.FirstOrDefault(changed => changed.Patch.PlacementId == placementId);

    public StoredPlacement? LiveOf(AreaKey area, string? placementId) =>
        placementId is null ? null : _live.GetValueOrDefault(area, []).FirstOrDefault(placement => placement.Id == placementId);

    public void Set(StoredPlacement wanted, string? scopeId = null)
    {
        AreaKey area = AreaOf(wanted.Surface, scopeId);
        StoredPlacement? live = LiveOf(area, wanted.Id);
        PlacementPatch? patch = live is null ? Added(area, wanted) : Changed(area, live, wanted);
        Put(wanted.Id, patch is null ? null : patch with { Base = _patches.GetValueOrDefault(wanted.Id)?.Base ?? live });
    }

    public void Remove(ArrangedSurface entry, string? scopeId = null)
    {
        if (entry.Placement is not { } placement)
            return;
        AreaKey area = AreaOf(placement.Surface, scopeId);
        StoredPlacement? live = LiveOf(area, placement.Id);
        Put(placement.Id, live is null ? null : new PlacementPatch(placement.Id, area.Area, area.ScopeId, placement.Surface, PatchKind.Remove, Base: live));
    }

    public void Discard(string placementId) => Put(placementId, null);

    public bool StepBack() => Replay(_back, _forward);

    public bool StepForward() => Replay(_forward, _back);

    public (List<PatchMessage> Set, List<string> Cleared) TakeUnsaved()
    {
        List<PatchMessage> set = _dirty.Where(_patches.ContainsKey).Select(id => _patches[id].ToMessage()).ToList();
        List<string> cleared = _dirty.Where(id => !_patches.ContainsKey(id)).ToList();
        _dirty.Clear();
        return (set, cleared);
    }

    public void Forget(string placementId)
    {
        _dirty.Remove(placementId);
        if (Draft.Patches().FirstOrDefault(patch => patch.PlacementId == placementId) is { } saved)
            _patches[placementId] = saved;
        else
            _patches.Remove(placementId);
    }

    public void MarkUnsaved(IEnumerable<PatchMessage> set, IEnumerable<string> cleared) =>
        _dirty.UnionWith(set.Select(patch => patch.PlacementId).Concat(cleared));

    public void Accept(Draft saved)
    {
        Draft = saved;
        foreach (string id in _patches.Keys.Where(id => !_dirty.Contains(id)).ToList())
            _patches.Remove(id);
        foreach (PlacementPatch patch in saved.Patches().Where(patch => !_dirty.Contains(patch.PlacementId)))
            _patches[patch.PlacementId] = patch;
    }

    private AreaKey AreaOf(string surfaceKey, string? scopeId) => new(surfaces.ByKey[surfaceKey].Area.Key, scopeId);

    private IEnumerable<PlacementPatch> InArea(AreaKey area) => _patches.Values.Where(patch => patch.Touches(area.Area, area.ScopeId));

    private bool Replay(List<Step> from, List<Step> to)
    {
        if (from.Count == 0)
            return false;
        Step step = from[^1];
        from.RemoveAt(from.Count - 1);
        Dictionary<string, PlacementPatch?> now = step.Before.Keys.ToDictionary(id => id, id => _patches.GetValueOrDefault(id));
        foreach ((string id, PlacementPatch? before) in step.Before)
            Write(id, before);
        to.Add(new Step(now, DateTimeOffset.MinValue));
        return true;
    }

    private void Remember(string placementId)
    {
        DateTimeOffset now = _time.GetUtcNow();
        _forward.Clear();
        if (_back.Count > 0 && _back[^1] is var last && (now - last.At < OneGesture || (last.Before.Count == 1 && last.Before.ContainsKey(placementId) && now - last.At < Typing)))
        {
            last.Before.TryAdd(placementId, _patches.GetValueOrDefault(placementId));
            _back[^1] = last with { At = now };
            return;
        }
        _back.Add(new Step(new Dictionary<string, PlacementPatch?> { [placementId] = _patches.GetValueOrDefault(placementId) }, now));
        if (_back.Count > MostSteps)
            _back.RemoveAt(0);
    }

    private void Put(string placementId, PlacementPatch? patch)
    {
        Remember(placementId);
        Write(placementId, patch);
    }

    private void Write(string placementId, PlacementPatch? patch)
    {
        Edits++;
        if (patch is null)
            _patches.Remove(placementId);
        else
            _patches[placementId] = patch;
        _dirty.Add(placementId);
    }

    private sealed record Step(Dictionary<string, PlacementPatch?> Before, DateTimeOffset At);

    private static PlacementPatch Added(AreaKey area, StoredPlacement wanted) =>
        new(wanted.Id, area.Area, area.ScopeId, wanted.Surface, PatchKind.Add, wanted.Order, wanted.Hidden, wanted.Audience,
            SetsDates: true, wanted.StartsAt, wanted.EndsAt, wanted.Values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value));

    private static PlacementPatch? Changed(AreaKey area, StoredPlacement live, StoredPlacement wanted)
    {
        Dictionary<string, string?> values = wanted.Values.Keys.Union(live.Values.Keys)
            .Where(key => wanted.Values.GetValueOrDefault(key) != live.Values.GetValueOrDefault(key))
            .ToDictionary(key => key, key => wanted.Values.GetValueOrDefault(key));
        bool dates = wanted.StartsAt != live.StartsAt || wanted.EndsAt != live.EndsAt;
        PlacementPatch patch = new(wanted.Id, area.Area, area.ScopeId, wanted.Surface, PatchKind.Change,
            wanted.Order == live.Order ? null : wanted.Order,
            wanted.Hidden == live.Hidden ? null : wanted.Hidden,
            wanted.Audience == live.Audience ? null : wanted.Audience,
            dates, dates ? wanted.StartsAt : null, dates ? wanted.EndsAt : null, values);
        return patch.Fields.Any() ? patch : null;
    }
}
