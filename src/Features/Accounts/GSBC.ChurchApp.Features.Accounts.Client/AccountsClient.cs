using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Accounts.Client;

public sealed class AccountsClient(ChannelBase channel)
{
    private readonly ISessionService _session = channel.CreateGrpcService<ISessionService>();

    public ValueTask<CurrentSession> GetCurrentSessionAsync() => _session.GetCurrentAsync(new GetCurrentSessionRequest());
}
