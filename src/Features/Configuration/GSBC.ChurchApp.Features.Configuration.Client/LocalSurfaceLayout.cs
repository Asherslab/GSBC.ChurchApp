using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Configuration;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Client;

internal sealed class LocalSurfaceLayout : ISurfaceLayout, IDisposable
{
    private const int MostPlacementsInAnArea = 500;
    private const int MostDrafts = 200;
    private static readonly TimeSpan FurthestBoundary = TimeSpan.FromDays(1);

    private readonly ILocalFeeds _feeds;
    private readonly IAccess _access;
    private readonly IPreviewMode _preview;
    private readonly SurfaceCatalog _surfaces;
    private readonly TimeProvider _time;
    private ITimer? _boundary;

    public LocalSurfaceLayout(ILocalFeeds feeds, IAccess access, IPreviewMode preview, SurfaceCatalog surfaces, TimeProvider time)
    {
        _feeds = feeds;
        _access = access;
        _preview = preview;
        _surfaces = surfaces;
        _time = time;
        _feeds.FeedsChanged += OnFeedsChanged;
        _access.Changed += OnChanged;
        _preview.Changed += OnChanged;
    }

    public event Action? Changed;

    public async Task<IReadOnlyList<ArrangedSurface>> ShownAsync(SurfaceArea area, string? scopeId = null)
    {
        await _preview.LoadAsync();
        PreviewChoice? preview = _preview.Current;
        DateTime now = preview?.At ?? _time.GetUtcNow().UtcDateTime;

        IReadOnlyList<StoredRecord> records = await _feeds.ReadPageAsync(new FeedPageQuery(
            ConfigurationFeeds.Placements, Limit: MostPlacementsInAnArea, GroupKey: ConfigurationFeeds.GroupOf(area.Key, scopeId)));
        IEnumerable<StoredPlacement> live = records.Select(record => ConfigurationContractsModel.Instance.Deserialize<Placement>(Read(record)).ToStored());

        EventLayer? layer = await LayerAsync();
        if (layer is not null && preview?.At is null)
            WatchBoundary(layer, now);

        IReadOnlyList<StoredPlacement> composed = DraftComposition.Compose(
            live,
            layer is null ? null : layer with { Patches = layer.Patches.Where(patch => patch.Touches(area.Key, scopeId)).ToList() },
            (await PreviewedAsync(preview)).Where(patch => patch.Touches(area.Key, scopeId)),
            now);

        Viewer viewer = preview?.As switch
        {
            PreviewViewer.Visitor => Viewer.Anonymous,
            PreviewViewer.Member => new Viewer(SignedIn: true, Holds: _ => false),
            _ => new Viewer(_access.SignedIn, node => _access.Can(area.AudiencePermission(node), scopeId)),
        };
        return SurfaceLayout.ShownTo(_surfaces.In(area), composed, viewer, now);
    }

    public void Dispose()
    {
        _feeds.FeedsChanged -= OnFeedsChanged;
        _access.Changed -= OnChanged;
        _preview.Changed -= OnChanged;
        _boundary?.Dispose();
    }

    private async Task<EventLayer?> LayerAsync()
    {
        IReadOnlyList<StoredRecord> records = await _feeds.ReadPageAsync(new FeedPageQuery(
            ConfigurationFeeds.Placements, Limit: 1, GroupKey: ConfigurationFeeds.EventLayerRecord));
        return records.Count == 0 ? null : ConfigurationContractsModel.Instance.Deserialize<EventLayerMessage>(Read(records[0])).ToLayer();
    }

    private async Task<IReadOnlyList<PlacementPatch>> PreviewedAsync(PreviewChoice? preview)
    {
        if (preview?.DraftId is not { } draftId)
            return [];
        IReadOnlyList<StoredRecord> drafts = await _feeds.ReadPageAsync(new FeedPageQuery(ConfigurationFeeds.Drafts, Limit: MostDrafts));
        return drafts.FirstOrDefault(record => record.Id == draftId) is { } found ? ConfigurationContractsModel.Instance.Deserialize<Draft>(Read(found)).Patches() : [];
    }

    private void WatchBoundary(EventLayer layer, DateTime now)
    {
        DateTime? next = new[] { layer.StartsAt, layer.EndsAt }.Where(moment => moment > now).Min();
        _boundary?.Dispose();
        _boundary = next is { } at && at - now < FurthestBoundary
            ? _time.CreateTimer(_ => Changed?.Invoke(), null, at - now + TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan)
            : null;
    }

    private static MemoryStream Read(StoredRecord record) => new(record.Payload);

    private void OnFeedsChanged(IReadOnlySet<string> feeds)
    {
        if (feeds.Contains(ConfigurationFeeds.Placements) || feeds.Contains(ConfigurationFeeds.Drafts))
            Changed?.Invoke();
    }

    private void OnChanged() => Changed?.Invoke();
}
