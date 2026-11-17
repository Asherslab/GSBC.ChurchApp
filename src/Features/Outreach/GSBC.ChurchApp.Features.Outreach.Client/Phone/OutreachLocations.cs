using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Locations;
using GSBC.ChurchApp.Platform.Ui.Locations;
using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed class OutreachLocations(OutreachStore store, RegionMaps maps) : ILocationSource, IKeptMapCover
{
    public IReadOnlyCollection<string> Feeds { get; } = [OutreachFeeds.Days, Maps.Contracts.OutreachMapsFeeds.Regions];

    public async Task<IReadOnlyList<Location>> LocationsAsync() =>
        [.. (await store.LoadAsync()).Days.Select(day => day.Meeting).OfType<Location>()];

    public async Task<MapSource?> CoverAsync(double latitude, double longitude)
    {
        if (!maps.KeepsFiles)
            return null;
        Routing.GeoPoint point = new(latitude, longitude);
        IReadOnlyList<RegionOnPhone> regions = await maps.ListAsync((await store.LoadAsync()).Regions);
        return regions.Where(region => region.Map?.State == Platform.Client.Files.KeptFileState.Done && RegionMaps.Bounds(region.Region.Bounds).Contains(point))
            .Select(maps.SourceFor).FirstOrDefault();
    }
}
