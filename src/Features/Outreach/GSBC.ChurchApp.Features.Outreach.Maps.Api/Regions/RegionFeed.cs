using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Storage;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal static class RegionFeed
{
    public const string Attribution =
        "Map © OpenStreetMap contributors (ODbL) · Protomaps. Addresses © State of Queensland (CC BY 4.0). " +
        "Land use © City of Moreton Bay (CC BY 4.0). Zoning © Brisbane City Council (CC BY 4.0).";

    public static OutreachRegion ToMessage(RegionRecord region, IBlobStore blobs) => new()
    {
        Id = region.Id,
        AreaId = region.AreaId,
        Name = region.Name,
        Core = new MapBounds { South = region.CoreSouth, West = region.CoreWest, North = region.CoreNorth, East = region.CoreEast },
        Bounds = new MapBounds { South = region.South, West = region.West, North = region.North, East = region.East },
        Version = region.Version,
        BuiltAt = region.BuiltAt,
        Streets = new MapFile { Url = blobs.PublicUrlOf(region.StreetsKey).ToString(), Bytes = region.StreetsBytes },
        Map = new MapFile { Url = blobs.PublicUrlOf(region.MapKey).ToString(), Bytes = region.MapBytes },
        MapMaxZoom = region.MapMaxZoom,
        Letterboxes = region.Letterboxes,
        Attribution = Attribution,
    };

    public static FeedChangeRecord Changed(RegionRecord region, IBlobStore blobs, DateTime now) => new()
    {
        Feed = OutreachMapsFeeds.Regions,
        RecordId = region.Id,
        Version = now.Ticks,
        Payload = Encode(ToMessage(region, blobs)),
        SortKey = region.Id,
    };

    public static FeedChangeRecord Removed(string regionId, DateTime now) => FeedChangeRecord.Marker(OutreachMapsFeeds.Regions, regionId, now, deleted: true);

    public static FeedChangeRecord BuildChanged(string areaId, DateTime now) => FeedChangeRecord.Marker(OutreachMapsFeeds.Builds, areaId, now);

    private static byte[] Encode<T>(T message)
    {
        using MemoryStream stream = new();
        OutreachMapsContractsModel.Instance.Serialize(stream, message);
        return stream.ToArray();
    }
}
