using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Sync;

[Service("gsbc.sync.Sync")]
public interface ISyncService
{
    [Public]
    ValueTask<PullReply> PullAsync(PullRequest request, CallContext context = default);

    [Public]
    IAsyncEnumerable<WatchEvent> WatchAsync(WatchRequest request, CallContext context = default);
}
