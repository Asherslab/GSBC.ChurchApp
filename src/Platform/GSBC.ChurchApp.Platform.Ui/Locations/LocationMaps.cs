using GSBC.ChurchApp.Platform.Client.Files;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Signals;
using GSBC.ChurchApp.Platform.Contracts.Locations;
using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Platform.Ui.Locations;

public sealed class LocationMaps(IKeptFiles kept, IEnumerable<IKeptMapCover> covers, IEnumerable<ILocationSource> sources, ClientSignals signals)
    : IClientStartupTask, IDisposable
{
    private readonly SemaphoreSlim _keeping = new(1, 1);

    public async Task<MapSource?> SourceAsync(Location location)
    {
        if (!location.HasPin || location.AddressHidden || location.Map is not { Url.Length: > 0 } map)
            return null;
        if (!kept.IsAvailable)
            return MapSource.Url(map.Url, map.Attribution);
        return await CoverAsync(location) ?? MapSource.KeptFile(PlaceMapPlan.FileId(map), map.Attribution, map.Url);
    }

    public async Task RunAsync()
    {
        if (!kept.IsAvailable)
            return;
        signals.Changed += OnChanged;
        signals.Follow([.. sources.SelectMany(source => source.Feeds)]);
        await KeepAsync();
    }

    public async Task KeepAsync()
    {
        if (!kept.IsAvailable)
            return;
        await _keeping.WaitAsync();
        try
        {
            List<Location> locations = [];
            foreach (ILocationSource source in sources)
                locations.AddRange(await source.LocationsAsync());
            HashSet<Location> covered = [];
            foreach (Location location in locations.Where(location => location.HasPin))
                if (await CoverAsync(location) is not null)
                    covered.Add(location);
            PlaceMapChanges changes = PlaceMapPlan.Plan(locations, covered.Contains, (await kept.ListAsync()).Select(file => file.Id));
            foreach (PlaceMapFile file in changes.Keep)
                await kept.KeepAsync(file.Id, file.Url, file.Title);
            foreach (string id in changes.Remove)
                await kept.RemoveAsync(id);
        }
        finally
        {
            _keeping.Release();
        }
    }

    public void Dispose()
    {
        signals.Changed -= OnChanged;
        _keeping.Dispose();
    }

    private async Task<MapSource?> CoverAsync(Location location)
    {
        foreach (IKeptMapCover cover in covers)
            if (await cover.CoverAsync(location.Latitude!.Value, location.Longitude!.Value) is { } source)
                return source;
        return null;
    }

    private void OnChanged(ClientChange change)
    {
        if (change.Concerns(ChangeKind.Feeds | ChangeKind.Session | ChangeKind.Access, [.. sources.SelectMany(source => source.Feeds)]))
            _ = KeepAsync();
    }
}
