using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Configuration.Client;

[AdminOnly]
public sealed class ConfigurationClient(ChannelBase channel)
{
    private readonly IConfigurationService _configuration = channel.CreateGrpcService<IConfigurationService>();

    public ValueTask<AreaPlacements> GetAreaAsync(SurfaceArea area, string? scopeId) =>
        _configuration.GetAreaAsync(new AreaQuery { Area = area.Key, ScopeId = scopeId });

    public ValueTask<ChangeList> ListChangesAsync(SurfaceArea area, string? scopeId) =>
        _configuration.ListChangesAsync(new AreaQuery { Area = area.Key, ScopeId = scopeId });

    public ValueTask<AreaPlacements> RollBackAsync(string batchId) =>
        _configuration.RollBackAsync(new RollBackRequest { BatchId = batchId });
}
