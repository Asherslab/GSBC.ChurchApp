using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.LandUse;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal sealed class AreaBuilder(OutreachMapsDbContext db, IMapSources sources, IBlobStore blobs, TimeProvider time, IOptions<MapSourcesOptions> options)
{
    private const double StreetMarginMetres = 500;
    private const string MapContentType = "application/vnd.pmtiles";
    private const string MapExtension = "pmtiles";
    private static readonly string StreetsExtension = StreetGraphFile.Extension.TrimStart('.');

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async Task BuildAsync(MapAreaRecord area, CancellationToken cancellationToken)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        GeoBounds bounds = new(area.South, area.West, area.North, area.East);
        List<RegionCell> cells = RegionGrid.Cut(area.Id, bounds);
        GeoBounds reach = bounds.Grown(RegionGrid.OverlapMetres);

        AreaStreets streets = await PlaceLetterboxesAsync(area, reach, bounds.Centre, cancellationToken);
        IRangeReader tiles = await sources.TilesAsync(cancellationToken);

        List<(RegionCell Cell, string Name)> kept = Named(cells.Where(cell => streets.HomesIn(cell.Core) > 0).Select(cell => (cell, streets.NameFor(cell.Core))).ToList(), area.Name);
        long streetsBytes = 0;
        long mapBytes = 0;
        for (int i = 0; i < kept.Count; i++)
        {
            await StageAsync(area, $"Building {kept[i].Name} ({i + 1} of {kept.Count})", cancellationToken);
            RegionRecord region = await PublishAsync(area, kept[i].Cell, kept[i].Name, streets, tiles, cancellationToken);
            streetsBytes += region.StreetsBytes;
            mapBytes += region.MapBytes;
        }
        await RemoveAsync(area.Id, kept.Select(pair => pair.Cell.Id).ToHashSet(), cancellationToken);
        await ForgetOldFilesAsync(cancellationToken);

        area.State = MapAreaStates.Built;
        area.Stage = null;
        area.Problem = null;
        area.BuiltAt = Now;
        PlacementCounts counts = streets.Counts;
        area.Report = JsonSerializer.Serialize(new MapAreaReport
        {
            Streets = streets.Network.Segments.Count,
            Addresses = counts.Addresses,
            Placed = counts.Placed,
            Homes = counts.Homes,
            Checks = counts.Checks,
            Skipped = counts.Skipped,
            FootpathsBesideRoads = streets.Network.PathsBesideRoads,
            StreetsBytes = streetsBytes,
            MapBytes = mapBytes,
            Seconds = (int)elapsed.Elapsed.TotalSeconds,
        });
        db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, Now));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AreaStreets> PlaceLetterboxesAsync(MapAreaRecord area, GeoBounds reach, GeoPoint origin, CancellationToken cancellationToken)
    {
        await StageAsync(area, "Fetching streets from OpenStreetMap", cancellationToken);
        OsmData osm = await sources.StreetsAsync(reach.Grown(StreetMarginMetres), cancellationToken);
        await StageAsync(area, "Fetching addresses", cancellationToken);
        IReadOnlyList<AddressPoint> addresses = await sources.AddressesAsync(reach, cancellationToken);
        await StageAsync(area, "Fetching land use and zoning", cancellationToken);
        LandUseMap landUse = await sources.LandUseAsync(reach, cancellationToken);
        await StageAsync(area, "Placing letterboxes on streets", cancellationToken);
        return AreaStreets.Build(osm, addresses, landUse, origin);
    }

    private static List<(RegionCell Cell, string Name)> Named(List<(RegionCell Cell, string Name)> cells, string areaName)
    {
        Dictionary<string, int> uses = cells.GroupBy(cell => cell.Name).ToDictionary(group => group.Key, group => group.Count());
        Dictionary<string, int> seen = [];
        return cells.Select(cell =>
        {
            string name = cell.Name.Length == 0 ? areaName : cell.Name;
            if (uses.GetValueOrDefault(cell.Name) < 2 && cell.Name.Length > 0)
                return (cell.Cell, name);
            int number = seen[name] = seen.GetValueOrDefault(name) + 1;
            return (cell.Cell, $"{name} {number}");
        }).ToList();
    }

    private async Task<RegionRecord> PublishAsync(MapAreaRecord area, RegionCell cell, string name, AreaStreets streets, IRangeReader tiles, CancellationToken cancellationToken)
    {
        DateTime now = Now;
        string fingerprint = Sha256(StreetGraphFile.ToBytes(streets.GraphFor(cell, RegionOf(cell, name, 0, DateTimeOffset.UnixEpoch))));
        string mapFile = Path.Combine(Path.GetTempPath(), $"gsbc-outreach-{Guid.NewGuid():N}.pmtiles");
        try
        {
            ExtractedTiles extracted;
            await using (FileStream output = File.Create(mapFile))
                extracted = await PmtilesExtract.ExtractAsync(tiles, cell.Bounds, options.Value.MaxZoom, output, cancellationToken);
            string mapKey = ContentAddress.KeyFor(await Sha256Async(mapFile, cancellationToken), MapExtension);

            RegionRecord? region = await db.Regions.FindAsync([cell.Id], cancellationToken);
            long version = RegionVersions.Next(region, fingerprint, mapKey);
            if (region is not null && version == region.Version)
            {
                if (region.Name != name)
                {
                    region.Name = name;
                    db.FeedChanges.Add(RegionFeed.Changed(region, blobs, now));
                    await db.SaveChangesAsync(cancellationToken);
                }
                return region;
            }

            StreetGraph graph = streets.GraphFor(cell, RegionOf(cell, name, version, new DateTimeOffset(now)));
            byte[] streetBytes = StreetGraphFile.ToBytes(graph);
            string streetsKey = ContentAddress.KeyFor(Sha256(streetBytes), StreetsExtension);
            await PutAsync(streetsKey, () => new MemoryStream(streetBytes), StreetGraphFile.ContentType, cancellationToken);
            await PutAsync(mapKey, () => File.OpenRead(mapFile), MapContentType, cancellationToken);

            if (region is null)
            {
                region = new RegionRecord { Id = cell.Id, AreaId = area.Id, Name = name, StreetsKey = streetsKey, StreetsFingerprint = fingerprint, MapKey = mapKey };
                db.Regions.Add(region);
            }
            region.Name = name;
            (region.CoreSouth, region.CoreWest, region.CoreNorth, region.CoreEast) = (cell.Core.South, cell.Core.West, cell.Core.North, cell.Core.East);
            (region.South, region.West, region.North, region.East) = (cell.Bounds.South, cell.Bounds.West, cell.Bounds.North, cell.Bounds.East);
            region.Version = version;
            region.BuiltAt = now;
            region.StreetsKey = streetsKey;
            region.StreetsFingerprint = fingerprint;
            region.StreetsBytes = streetBytes.Length;
            region.MapKey = mapKey;
            region.MapBytes = extracted.Bytes;
            region.MapMaxZoom = extracted.MaxZoom;
            region.Letterboxes = Enumerable.Range(0, graph.StreetCount).Sum(graph.Homes);
            db.Files.Add(new RegionFileRecord { Key = streetsKey, RegionId = region.Id, Version = version });
            if (mapKey != streetsKey)
                db.Files.Add(new RegionFileRecord { Key = mapKey, RegionId = region.Id, Version = version });
            db.FeedChanges.Add(RegionFeed.Changed(region, blobs, now));
            await db.SaveChangesAsync(cancellationToken);
            return region;
        }
        finally
        {
            File.Delete(mapFile);
        }
    }

    private static StreetRegion RegionOf(RegionCell cell, string name, long version, DateTimeOffset built) =>
        new(cell.Id, name, version, built, cell.Bounds, cell.Core, cell.Core.Centre, RegionFeed.Attribution);

    public async Task RemoveAsync(string areaId, IReadOnlySet<string> kept, CancellationToken cancellationToken)
    {
        List<RegionRecord> gone = await db.Regions.Where(region => region.AreaId == areaId && !kept.Contains(region.Id)).ToListAsync(cancellationToken);
        foreach (RegionRecord region in gone)
        {
            db.Regions.Remove(region);
            db.FeedChanges.Add(RegionFeed.Removed(region.Id, Now));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgetOldFilesAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, RegionRecord> regions = await db.Regions.ToDictionaryAsync(region => region.Id, cancellationToken);
        List<RegionFileRecord> files = await db.Files.ToListAsync(cancellationToken);
        List<RegionFileRecord> old = files.Where(file => !RegionVersions.Kept(file, regions.GetValueOrDefault(file.RegionId))).ToList();
        if (old.Count == 0)
            return;
        db.Files.RemoveRange(old);
        await db.SaveChangesAsync(cancellationToken);
        HashSet<string> stillUsed = files.Except(old).Select(file => file.Key).ToHashSet();
        foreach (string key in old.Select(file => file.Key).Distinct().Where(key => !stillUsed.Contains(key)))
            await blobs.DeleteAsync(BlobBucket.Public, key, cancellationToken);
    }

    private async Task PutAsync(string key, Func<Stream> open, string contentType, CancellationToken cancellationToken)
    {
        if (await blobs.FindAsync(BlobBucket.Public, key, cancellationToken) is not null)
            return;
        await using Stream content = open();
        await blobs.PutAsync(BlobBucket.Public, key, content, new BlobHeaders(contentType, ContentAddress.ImmutableCacheControl), cancellationToken);
    }

    private async Task StageAsync(MapAreaRecord area, string stage, CancellationToken cancellationToken)
    {
        area.Stage = stage;
        db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, Now));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Sha256(byte[] bytes) => ContentAddress.Sha256Of(bytes);

    private static async Task<string> Sha256Async(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
    }
}
