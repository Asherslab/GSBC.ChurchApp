using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

[Service("gsbc.ministries.Ministries")]
public interface IMinistriesService
{
    [Public]
    ValueTask<MinistryDirectory> DirectoryAsync(DirectoryRequest request, CallContext context = default);

    [Public]
    ValueTask<MinistryPage> MinistryAsync(MinistryRef request, CallContext context = default);

    [Public]
    ValueTask<LifeGroupsPage> LifeGroupsAsync(LifeGroupsRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<JoinOutcome> JoinAsync(JoinRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> LeaveAsync(JoinRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> WithdrawAsync(AskRef request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> ParentAnswerAsync(AskRef request, CallContext context = default);

    [SignedIn]
    ValueTask<WaitingList> WaitingAsync(WaitingRequest request, CallContext context = default);
}
