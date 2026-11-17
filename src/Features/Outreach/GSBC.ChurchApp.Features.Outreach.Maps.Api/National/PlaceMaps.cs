using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Api.Locations;
using GSBC.ChurchApp.Platform.Contracts.Locations;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.National;

internal sealed class PlaceMaps(OutreachMapsDbContext db, IMapSources sources, IBlobStore blobs, TimeProvider time, IOptions<MapSourcesOptions> options,
    IEnumerable<ILocationUses> uses)
{
    public const int Zoom = 15;
    private const int Tiles = 2;
    private const double Inside = 1e-7;
    private const string MapContentType = "application/vnd.pmtiles";
    private const string MapExtension = "pmtiles";
    private static readonly TimeSpan KeptAtLeast = TimeSpan.FromDays(2);

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public static (string Id, GeoBounds Bounds) BlockAround(double latitude, double longitude)
    {
        double size = 1 << Zoom;
        double radians = latitude * Math.PI / 180;
        double x = (longitude + 180) / 360 * size;
        double y = (1 - Math.Log(Math.Tan(radians) + 1 / Math.Cos(radians)) / Math.PI) / 2 * size;
        int left = (int)Math.Floor(x - Tiles / 2.0 + 0.5);
        int top = (int)Math.Floor(y - Tiles / 2.0 + 0.5);
        return ($"{Zoom}-{left}-{top}", new GeoBounds(LatitudeOf(top + Tiles, size), left / size * 360 - 180, LatitudeOf(top, size), (left + Tiles) / size * 360 - 180));
    }

    public async Task<LocationMap> PrepareAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        if (!OverviewMap.Australia.Contains(new GeoPoint(latitude, longitude)))
            return new LocationMap();
        (string id, GeoBounds bounds) = BlockAround(latitude, longitude);
        if (await db.PlaceMaps.AsNoTracking().SingleOrDefaultAsync(map => map.Id == id, cancellationToken) is { } made
            && await blobs.FindAsync(BlobBucket.Public, made.MapKey, cancellationToken) is not null)
            return Message(made);

        IRangeReader tiles = await sources.TilesAsync(cancellationToken);
        string file = Path.Combine(Path.GetTempPath(), $"gsbc-place-{Guid.NewGuid():N}.pmtiles");
        try
        {
            ExtractedTiles extracted;
            GeoBounds cut = new(bounds.South + Inside, bounds.West + Inside, bounds.North - Inside, bounds.East - Inside);
            await using (FileStream output = File.Create(file))
                extracted = await PmtilesExtract.ExtractAsync(tiles, cut, Math.Min(Zoom, options.Value.MaxZoom), output, cancellationToken);
            string key = ContentAddress.KeyFor(await OverviewMap.Sha256Async(file, cancellationToken), MapExtension);
            if (await blobs.FindAsync(BlobBucket.Public, key, cancellationToken) is null)
            {
                await using FileStream content = File.OpenRead(file);
                await blobs.PutAsync(BlobBucket.Public, key, content, new BlobHeaders(MapContentType, ContentAddress.ImmutableCacheControl), cancellationToken);
            }
            PlaceMapRecord record = await db.PlaceMaps.FindAsync([id], cancellationToken) ?? db.PlaceMaps.Add(new PlaceMapRecord { Id = id, MapKey = key }).Entity;
            (record.MapKey, record.MapBytes, record.MaxZoom, record.BuiltAt) = (key, extracted.Bytes, extracted.MaxZoom, Now);
            (record.South, record.West, record.North, record.East) = (bounds.South, bounds.West, bounds.North, bounds.East);
            await db.SaveChangesAsync(cancellationToken);
            return Message(record);
        }
        finally
        {
            File.Delete(file);
        }
    }

    public async Task<int> SweepAsync(CancellationToken cancellationToken)
    {
        HashSet<string> used = [];
        foreach (ILocationUses use in uses)
            used.UnionWith(await use.MapKeysAsync(cancellationToken));
        DateTime before = Now - KeptAtLeast;
        List<PlaceMapRecord> unused = [.. (await db.PlaceMaps.Where(map => map.BuiltAt < before).ToListAsync(cancellationToken)).Where(map => !used.Contains(map.MapKey))];
        foreach (PlaceMapRecord map in unused)
        {
            db.PlaceMaps.Remove(map);
            await db.SaveChangesAsync(cancellationToken);
            if (!await db.PlaceMaps.AnyAsync(other => other.MapKey == map.MapKey, cancellationToken)
                && !await db.Files.AnyAsync(regionFile => regionFile.Key == map.MapKey, cancellationToken))
                await blobs.DeleteAsync(BlobBucket.Public, map.MapKey, cancellationToken);
        }
        return unused.Count;
    }

    private LocationMap Message(PlaceMapRecord map) => new()
    {
        Key = map.MapKey,
        Url = blobs.PublicUrlOf(map.MapKey).ToString(),
        South = map.South, West = map.West, North = map.North, East = map.East,
        Bytes = map.MapBytes,
        MaxZoom = map.MaxZoom,
        Attribution = OverviewMap.Attribution,
    };

    private static double LatitudeOf(int y, double size) => Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / size))) * 180 / Math.PI;
}
