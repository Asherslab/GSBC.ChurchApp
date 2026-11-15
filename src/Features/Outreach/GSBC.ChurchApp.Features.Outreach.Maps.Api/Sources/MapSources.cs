using System.Globalization;
using System.Text.Json;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.LandUse;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;
using GSBC.ChurchApp.Features.Outreach.Routing;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;

internal interface IMapSources
{
    Task<OsmData> StreetsAsync(GeoBounds bounds, CancellationToken cancellationToken);

    Task<IReadOnlyList<AddressPoint>> AddressesAsync(GeoBounds bounds, CancellationToken cancellationToken);

    Task<LandUseMap> LandUseAsync(GeoBounds bounds, CancellationToken cancellationToken);

    Task<IRangeReader> TilesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OsmPlace>> PlacesAsync(CancellationToken cancellationToken);

    Task<int> CountAddressesAsync(GeoBounds bounds, CancellationToken cancellationToken);
}

internal sealed class MapSources(HttpClient http, IOptions<MapSourcesOptions> options) : IMapSources
{
    private const string PrimaryAddress = "address_status='P'";
    private const string AddressFields = "address_pid,street_full,unit_number,lotplan,locality,local_authority";
    private MapSourcesOptions Options => options.Value;

    public Task<OsmData> StreetsAsync(GeoBounds bounds, CancellationToken cancellationToken) =>
        WithExtractAsync(pbf => OsmPbf.ReadAsync(pbf, bounds, StreetNetwork.IsStreet, cancellationToken), cancellationToken);

    public async Task<IReadOnlyList<OsmPlace>> PlacesAsync(CancellationToken cancellationToken) =>
        await WithExtractAsync(pbf => OsmPbf.ReadPlacesAsync(pbf, cancellationToken), cancellationToken);

    public Task<int> CountAddressesAsync(GeoBounds bounds, CancellationToken cancellationToken) =>
        new ArcGisLayer(http).CountAsync(Options.Addresses, bounds, PrimaryAddress, cancellationToken);

    private async Task<T> WithExtractAsync<T>(Func<Stream, Task<T>> read, CancellationToken cancellationToken)
    {
        string file = Path.Combine(Path.GetTempPath(), $"gsbc-outreach-{Guid.NewGuid():N}.osm.pbf");
        try
        {
            await using (Stream download = await http.GetStreamAsync(Options.Streets, cancellationToken))
            await using (FileStream saved = File.Create(file))
                await download.CopyToAsync(saved, cancellationToken);
            await using FileStream pbf = File.OpenRead(file);
            return await read(pbf);
        }
        finally
        {
            File.Delete(file);
        }
    }

    public async Task<IReadOnlyList<AddressPoint>> AddressesAsync(GeoBounds bounds, CancellationToken cancellationToken)
    {
        List<ArcGisFeature> features = await new ArcGisLayer(http).QueryAsync(Options.Addresses, bounds, AddressFields, PrimaryAddress, "objectid", 4000, true, cancellationToken);
        return features.Where(feature => feature.Point is not null).Select(feature => new AddressPoint(
            uint.TryParse(feature.Attributes.GetValueOrDefault("address_pid"), NumberStyles.Integer, CultureInfo.InvariantCulture, out uint id) ? id : 0,
            feature.Point!.Value,
            feature.Attributes.GetValueOrDefault("street_full", ""),
            feature.Attributes.GetValueOrDefault("unit_number", "").Length > 0,
            feature.Attributes.GetValueOrDefault("lotplan", ""),
            feature.Attributes.GetValueOrDefault("locality", ""),
            feature.Attributes.GetValueOrDefault("local_authority", ""))).ToList();
    }

    public async Task<LandUseMap> LandUseAsync(GeoBounds bounds, CancellationToken cancellationToken)
    {
        ArcGisLayer layer = new(http);
        List<ArcGisFeature> lots = await layer.QueryAsync(Options.MoretonBayLots, bounds,
            $"{LandUseMap.MoretonBayLotPlan},{LandUseMap.MoretonBayLandUse}", "1=1", "OBJECTID", 2000, true, cancellationToken);
        List<ArcGisFeature> zones = await layer.QueryAsync(Options.BrisbaneZones, bounds,
            $"{LandUseMap.BrisbaneZoneCode},{LandUseMap.BrisbanePrecinct}", "1=1", "OBJECTID", 2000, true, cancellationToken);
        return new LandUseMap(lots.Select(ToParcel).ToList(), zones.Select(ToParcel).ToList());
    }

    public async Task<IRangeReader> TilesAsync(CancellationToken cancellationToken)
    {
        await using Stream stream = await http.GetStreamAsync(Options.TileBuilds, cancellationToken);
        using JsonDocument builds = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        string latest = builds.RootElement.EnumerateArray()
            .Select(build => build.GetProperty("key").GetString() ?? "")
            .Where(key => key.EndsWith(".pmtiles", StringComparison.Ordinal))
            .Max(StringComparer.Ordinal) ?? throw new InvalidOperationException("The tile builds list is empty");
        return new HttpRangeReader(http, new Uri(Options.TileBuildFiles, latest));
    }

    private static Parcel ToParcel(ArcGisFeature feature) => new(feature.Rings, feature.Attributes);
}
