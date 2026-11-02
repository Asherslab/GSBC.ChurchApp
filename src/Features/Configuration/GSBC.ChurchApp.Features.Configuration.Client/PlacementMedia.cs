using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Configuration.Client;

internal static class PlacementMedia
{
    private const int MostPlacements = 500;

    public static MediaPrefetch Prefetch(SurfaceCatalog surfaces) =>
        new(new FeedPageQuery(ConfigurationFeeds.Placements, Limit: MostPlacements), record => In(surfaces, record));

    private static IEnumerable<MediaRef> In(SurfaceCatalog surfaces, StoredRecord record)
    {
        Placement placement = ConfigurationContractsModel.Instance.Deserialize<Placement>(new MemoryStream(record.Payload));
        if (placement.Hidden || !surfaces.ByKey.TryGetValue(placement.Surface, out Surface? surface))
            return [];

        Dictionary<string, string> values = placement.Values.GroupBy(value => value.Key).ToDictionary(group => group.Key, group => group.Last().Value);
        return surface.Settings.OfType<IHoldsMedia>()
            .SelectMany(holder => holder.MediaIn(values.GetValueOrDefault(((Setting)holder).Key, "")));
    }
}
