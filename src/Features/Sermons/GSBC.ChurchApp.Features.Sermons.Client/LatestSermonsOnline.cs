using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Sermons.Client;

[AdminOnly]
public sealed class LatestSermonsOnline(ChannelBase channel) : ILatestSermonsOnline
{
    private readonly ISermonsService _sermons = channel.CreateGrpcService<ISermonsService>();

    public async Task<IReadOnlyList<Sermon>> LatestAsync(int count) => (await _sermons.LatestAsync(new LatestQuery { Count = count })).Sermons;
}
