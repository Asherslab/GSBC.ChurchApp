using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed class OfflineMaps(OutreachStore store, RegionMaps regions)
{
    public static readonly IReadOnlyList<string> Feeds = [OutreachMapsFeeds.Regions];

    public async Task<IReadOnlyList<RegionOnPhone>> LoadAsync() =>
        [.. (await regions.ListAsync((await store.LoadAsync()).Regions)).OrderByDescending(region => region.Kept).ThenBy(region => region.Region.Name)];

    public Task KeepAsync(OutreachRegion region) => regions.KeepAsync(region, true);

    public Task RemoveAsync(OutreachRegion region) => regions.RemoveAsync(region);
}
