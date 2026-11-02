using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Configuration;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Client;

internal sealed class LocalSurfaceLayout : ISurfaceLayout, IDisposable
{
    private const int MostPlacementsInAnArea = 500;

    private readonly ILocalFeeds _feeds;
    private readonly IAccess _access;
    private readonly SurfaceCatalog _surfaces;
    private readonly TimeProvider _time;

    public LocalSurfaceLayout(ILocalFeeds feeds, IAccess access, SurfaceCatalog surfaces, TimeProvider time)
    {
        _feeds = feeds;
        _access = access;
        _surfaces = surfaces;
        _time = time;
        _feeds.FeedsChanged += OnFeedsChanged;
        _access.Changed += OnAccessChanged;
    }

    public event Action? Changed;

    public async Task<IReadOnlyList<ArrangedSurface>> ShownAsync(SurfaceArea area, string? scopeId = null)
    {
        IReadOnlyList<StoredRecord> records = await _feeds.ReadPageAsync(new FeedPageQuery(
            ConfigurationFeeds.Placements, Limit: MostPlacementsInAnArea, GroupKey: ConfigurationFeeds.GroupOf(area.Key, scopeId)));

        IEnumerable<StoredPlacement> stored = records.Select(record =>
            ConfigurationContractsModel.Instance.Deserialize<Placement>(new MemoryStream(record.Payload)).ToStored());
        Viewer viewer = new(_access.SignedIn, node => _access.Can(area.AudiencePermission(node), scopeId));

        return SurfaceLayout.ShownTo(_surfaces.In(area), stored, viewer, _time.GetUtcNow().UtcDateTime);
    }

    public void Dispose()
    {
        _feeds.FeedsChanged -= OnFeedsChanged;
        _access.Changed -= OnAccessChanged;
    }

    private void OnFeedsChanged(IReadOnlySet<string> feeds)
    {
        if (feeds.Contains(ConfigurationFeeds.Placements))
            Changed?.Invoke();
    }

    private void OnAccessChanged() => Changed?.Invoke();
}
