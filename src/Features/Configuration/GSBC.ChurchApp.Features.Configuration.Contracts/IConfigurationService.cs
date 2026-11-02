using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

[Service("gsbc.config.Configuration")]
public interface IConfigurationService
{
    [SignedIn]
    ValueTask<AreaPlacements> GetAreaAsync(AreaQuery request, CallContext context = default);

    [SignedIn]
    ValueTask<AreaPlacements> SaveAsync(SaveRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<ChangeList> ListChangesAsync(AreaQuery request, CallContext context = default);

    [SignedIn]
    ValueTask<AreaPlacements> RollBackAsync(RollBackRequest request, CallContext context = default);
}
