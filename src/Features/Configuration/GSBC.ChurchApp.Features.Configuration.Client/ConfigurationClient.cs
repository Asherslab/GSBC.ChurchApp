using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Configuration.Client;

public sealed class ConfigurationClient(ChannelBase channel)
{
    private readonly IConfigurationService _configuration = channel.CreateGrpcService<IConfigurationService>();

    public ValueTask<AreaPlacements> GetAreaAsync(SurfaceArea area, string? scopeId) =>
        _configuration.GetAreaAsync(new AreaQuery { Area = area.Key, ScopeId = scopeId });

    public ValueTask<AreaPlacements> SaveAsync(SurfaceArea area, string? scopeId, IEnumerable<Placement> placements, IEnumerable<PlacementReference>? removed = null) =>
        _configuration.SaveAsync(new SaveRequest { Area = area.Key, ScopeId = scopeId, Placements = [.. placements], Removed = [.. removed ?? []] });

    public ValueTask<ChangeList> ListChangesAsync(SurfaceArea area, string? scopeId) =>
        _configuration.ListChangesAsync(new AreaQuery { Area = area.Key, ScopeId = scopeId });

    public ValueTask<AreaPlacements> RollBackAsync(string batchId) =>
        _configuration.RollBackAsync(new RollBackRequest { BatchId = batchId });
}
