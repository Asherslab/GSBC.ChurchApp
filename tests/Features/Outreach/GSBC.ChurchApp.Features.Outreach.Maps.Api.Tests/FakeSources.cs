using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.LandUse;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

internal sealed class FakeSources : IMapSources
{
    private readonly List<(long Id, string Name, (double X, double Y) From, (double X, double Y) To)> _streets = [];
    private readonly List<AddressPoint> _addresses = [];
    private uint _nextId = 1;

    public FakeSources()
    {
        AddStreet("Kingfisher Street", (0, 0), (300, 0));
        AddHome("Kingfisher Street", new Flat(60, 15), "12");
        AddHome("Kingfisher Street", new Flat(120, -15), "15");
    }

    public List<GeoBounds> StreetsAskedFor { get; } = [];

    public void AddStreet(string name, (double X, double Y) from, (double X, double Y) to) => _streets.Add((_streets.Count + 1, name, from, to));

    public void AddHome(Flat at) => AddHome("Kingfisher Street", at);

    public void AddHome(string street, Flat at, string number = "") =>
        _addresses.Add(new AddressPoint(_nextId++, Streets.Projection.ToGeo(at), street, false, "", "Albany Creek", "Brisbane City", number));

    public Task<OsmData> StreetsAsync(GeoBounds bounds, CancellationToken cancellationToken)
    {
        StreetsAskedFor.Add(bounds);
        Streets streets = new();
        foreach ((long id, string name, (double X, double Y) from, (double X, double Y) to) in _streets)
            streets.Way(id, "residential", name, from, to);
        return Task.FromResult(streets.Osm);
    }

    public Task<IReadOnlyList<AddressPoint>> AddressesAsync(GeoBounds bounds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AddressPoint>>([.. _addresses.Where(address => bounds.Contains(address.Point))]);

    public Task<LandUseMap> LandUseAsync(GeoBounds bounds, CancellationToken cancellationToken)
    {
        GeoPoint[] everywhere = [new(-28, 152), new(-28, 154), new(-27, 154), new(-27, 152)];
        Parcel residential = new([everywhere], new Dictionary<string, string> { [LandUseMap.BrisbaneZoneCode] = "LDR" });
        return Task.FromResult(new LandUseMap([], [residential]));
    }

    public Task<IRangeReader> TilesAsync(CancellationToken cancellationToken) => Task.FromResult<IRangeReader>(new BytesReader(TileArchives.World(6)));

    public Task<IReadOnlyList<OsmPlace>> PlacesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OsmPlace>>([]);

    public Task<int> CountAddressesAsync(GeoBounds bounds, CancellationToken cancellationToken) =>
        Task.FromResult(_addresses.Count(address => bounds.Contains(address.Point)));
}
