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
        if (record.GroupKey == ConfigurationFeeds.EventLayerRecord)
        {
            EventLayerMessage layer = ConfigurationContractsModel.Instance.Deserialize<EventLayerMessage>(new MemoryStream(record.Payload));
            return layer.Changes
                .Where(change => change.Kind != (int)PatchKind.Remove)
                .SelectMany(change => MediaIn(surfaces, change.Surface, change.Values.Where(value => value.Value is not null)
                    .GroupBy(value => value.Key).ToDictionary(group => group.Key, group => group.Last().Value!)));
        }

        Placement placement = ConfigurationContractsModel.Instance.Deserialize<Placement>(new MemoryStream(record.Payload));
        return placement.Hidden
            ? []
            : MediaIn(surfaces, placement.Surface, placement.Values.GroupBy(value => value.Key).ToDictionary(group => group.Key, group => group.Last().Value));
    }

    private static IEnumerable<MediaRef> MediaIn(SurfaceCatalog surfaces, string surfaceKey, Dictionary<string, string> values) =>
        surfaces.ByKey.TryGetValue(surfaceKey, out Surface? surface)
            ? surface.Settings.OfType<IHoldsMedia>().SelectMany(holder => holder.MediaIn(values.GetValueOrDefault(((Setting)holder).Key, "")))
            : [];
}
