using Grpc.Core;
using GSBC.ChurchApp.Features.People.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.People.Client;

public sealed class SafeMinistryCheckClient(ChannelBase channel)
{
    private readonly ISafeMinistryCheckService _smc = channel.CreateGrpcService<ISafeMinistryCheckService>();

    public ValueTask<SmcSyncStatus> GetStatusAsync() => _smc.GetStatusAsync(new SmcStatusRequest());

    public ValueTask<SmcSyncStatus> SyncNowAsync() => _smc.SyncNowAsync(new SmcSyncRequest());

    public async Task<IReadOnlyList<SmcPerson>> GetUnmatchedAsync() => (await _smc.GetUnmatchedAsync(new SmcUnmatchedRequest())).People;

    public ValueTask<Done> LinkAsync(string personId, string smcId) => _smc.LinkAsync(new SmcLinkRequest { PersonId = personId, SmcId = smcId });

    public ValueTask<Done> UnlinkAsync(string personId) => _smc.UnlinkAsync(new PersonRequest { PersonId = personId });
}
