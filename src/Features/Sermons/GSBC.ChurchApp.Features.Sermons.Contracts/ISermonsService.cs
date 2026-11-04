using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

[Service("gsbc.sermons.Sermons")]
public interface ISermonsService
{
    [Public]
    ValueTask<SermonPage> LatestAsync(LatestQuery request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> SaveProgressAsync(ProgressBatch request, CallContext context = default);
}
