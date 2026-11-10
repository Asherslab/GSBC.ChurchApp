using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Accounts.Client;

[AdminOnly]
public sealed class OnlineAccount(ChannelBase channel) : IAccountReads
{
    private readonly ISessionService _session = channel.CreateGrpcService<ISessionService>();
    private readonly IAccountsService _accounts = channel.CreateGrpcService<IAccountsService>();

    public IReadOnlyList<string> Feeds { get; } = [AccountsFeeds.People];

    public async Task<MyAccount> MineAsync() => new()
    {
        Profile = await _accounts.GetProfileAsync(new Nothing()),
        Passkeys = await _accounts.ListPasskeysAsync(new Nothing()),
        Sessions = await _accounts.ListSessionsAsync(new Nothing()),
    };

    public ValueTask<CurrentSession> GetCurrentSessionAsync() => _session.GetCurrentAsync(new GetCurrentSessionRequest());
}
