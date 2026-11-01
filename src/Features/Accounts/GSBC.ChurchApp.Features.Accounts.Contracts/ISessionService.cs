using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

[Service("gsbc.accounts.Session")]
public interface ISessionService
{
    [Public]
    ValueTask<CurrentSession> GetCurrentAsync(GetCurrentSessionRequest request, CallContext context = default);
}
