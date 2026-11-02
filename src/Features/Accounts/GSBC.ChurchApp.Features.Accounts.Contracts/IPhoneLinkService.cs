using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

[Service("gsbc.accounts.PhoneLinks")]
public interface IPhoneLinkService
{
    [SignedIn]
    ValueTask<CodeReply> StartAsync(Nothing request, CallContext context = default);

    [Public]
    ValueTask<LinkedPhone> LinkAsync(LinkPhoneRequest request, CallContext context = default);
}
