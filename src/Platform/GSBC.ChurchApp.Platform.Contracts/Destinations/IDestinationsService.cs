using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Destinations;

[Service("gsbc.destinations.Destinations")]
public interface IDestinationsService
{
    [SignedIn]
    ValueTask<DestinationResults> SearchAsync(DestinationSearch request, CallContext context = default);

    [Public]
    ValueTask<Destination> DescribeAsync(DestinationRequest request, CallContext context = default);
}
