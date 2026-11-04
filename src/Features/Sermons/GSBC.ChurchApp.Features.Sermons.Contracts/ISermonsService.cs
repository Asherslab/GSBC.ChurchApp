using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

[Service("gsbc.sermons.Sermons")]
public interface ISermonsService
{
    [SignedIn]
    ValueTask<Done> SaveProgressAsync(ProgressBatch request, CallContext context = default);
}
