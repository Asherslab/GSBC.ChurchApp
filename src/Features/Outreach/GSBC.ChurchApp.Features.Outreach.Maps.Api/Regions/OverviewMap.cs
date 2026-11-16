using System.Security.Cryptography;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal sealed class OverviewMap(OutreachMapsDbContext db, IMapSources sources, IBlobStore blobs, TimeProvider time, IOptions<MapSourcesOptions> options)
{
    public const string Id = "australia";
    public const string Attribution = "Map © OpenStreetMap contributors (ODbL) · Protomaps.";
    public static readonly GeoBounds Australia = new(-44.5, 112.5, -9, 154);
    private static readonly TimeSpan KeptFor = TimeSpan.FromDays(30);
    private const string MapContentType = "application/vnd.pmtiles";
    private const string MapExtension = "pmtiles";

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async Task<bool> DueAsync(CancellationToken cancellationToken) =>
        await db.Overviews.AsNoTracking().SingleOrDefaultAsync(overview => overview.Id == Id, cancellationToken) is not { } built
        || Now - built.BuiltAt > KeptFor;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IRangeReader tiles = await sources.TilesAsync(cancellationToken);
        string file = Path.Combine(Path.GetTempPath(), $"gsbc-outreach-{Guid.NewGuid():N}.pmtiles");
        try
        {
            ExtractedTiles extracted;
            await using (FileStream output = File.Create(file))
                extracted = await PmtilesExtract.ExtractAsync(tiles, Australia, options.Value.OverviewMaxZoom, output, cancellationToken);
            string key = ContentAddress.KeyFor(await Sha256Async(file, cancellationToken), MapExtension);
            if (await blobs.FindAsync(BlobBucket.Public, key, cancellationToken) is null)
            {
                await using FileStream content = File.OpenRead(file);
                await blobs.PutAsync(BlobBucket.Public, key, content, new BlobHeaders(MapContentType, ContentAddress.ImmutableCacheControl), cancellationToken);
            }

            OverviewRecord? overview = await db.Overviews.FindAsync([Id], cancellationToken);
            string? replaced = overview?.MapKey == key ? null : overview?.MapKey;
            if (overview is null)
                db.Overviews.Add(overview = new OverviewRecord { Id = Id, MapKey = key });
            (overview.MapKey, overview.MapBytes, overview.MaxZoom, overview.BuiltAt) = (key, extracted.Bytes, extracted.MaxZoom, Now);
            await db.SaveChangesAsync(cancellationToken);
            if (replaced is not null && !await db.Files.AnyAsync(regionFile => regionFile.Key == replaced, cancellationToken))
                await blobs.DeleteAsync(BlobBucket.Public, replaced, cancellationToken);
        }
        finally
        {
            File.Delete(file);
        }
    }

    public async Task<MapOverview> MessageAsync(CancellationToken cancellationToken) =>
        await db.Overviews.AsNoTracking().SingleOrDefaultAsync(overview => overview.Id == Id, cancellationToken) is { } built
            ? new MapOverview
            {
                Map = new MapFile { Url = blobs.PublicUrlOf(built.MapKey).ToString(), Bytes = built.MapBytes },
                Bounds = new MapBounds { South = Australia.South, West = Australia.West, North = Australia.North, East = Australia.East },
                MaxZoom = built.MaxZoom,
                BuiltAt = built.BuiltAt,
                Attribution = Attribution,
                Addable = [.. Queensland.Outline.Select(corner => new MapCorner { Latitude = corner.Lat, Longitude = corner.Lon })],
            }
            : new MapOverview();

    private static async Task<string> Sha256Async(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
    }
}
