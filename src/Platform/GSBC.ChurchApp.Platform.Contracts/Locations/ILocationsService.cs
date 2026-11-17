using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Locations;

[Service("gsbc.locations.Locations")]
public interface ILocationsService
{
    [SignedIn]
    ValueTask<FoundLocations> FindAsync(LocationSearch request, CallContext context = default);

    [SignedIn]
    ValueTask<FoundLocations> NearestAsync(LocationPoint request, CallContext context = default);

    [SignedIn]
    ValueTask<LocationMap> PrepareMapAsync(LocationPoint request, CallContext context = default);
}
