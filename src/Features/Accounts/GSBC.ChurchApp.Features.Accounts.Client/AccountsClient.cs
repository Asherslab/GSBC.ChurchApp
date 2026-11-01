using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Accounts.Client;

public sealed class AccountsClient(ChannelBase channel)
{
    private readonly ISessionService _session = channel.CreateGrpcService<ISessionService>();
    private readonly IAccountsService _accounts = channel.CreateGrpcService<IAccountsService>();

    public event Action? SessionChanged;

    public ValueTask<CurrentSession> GetCurrentSessionAsync() => _session.GetCurrentAsync(new GetCurrentSessionRequest());

    public ValueTask<TicketReply> StartSignUpAsync(string name, string email) =>
        _accounts.StartSignUpAsync(new StartSignUpRequest { DisplayName = name, Email = email });

    public ValueTask<TicketReply> RedeemAsync(string code) => _accounts.RedeemAsync(new RedeemRequest { Code = code });

    public ValueTask<Done> RequestRecoveryAsync(string email) => _accounts.RequestRecoveryAsync(new RequestRecoveryRequest { Email = email });

    public ValueTask<Profile> GetProfileAsync() => _accounts.GetProfileAsync(new Nothing());

    public ValueTask<Done> UpdateNameAsync(string name) => _accounts.UpdateNameAsync(new UpdateNameRequest { DisplayName = name });

    public ValueTask<Done> ConfirmEmailAsync(string code) => _accounts.ConfirmEmailAsync(new ConfirmEmailRequest { Code = code });

    public ValueTask<Done> ResendConfirmationAsync() => _accounts.ResendConfirmationAsync(new Nothing());

    public ValueTask<PasskeyList> ListPasskeysAsync() => _accounts.ListPasskeysAsync(new Nothing());

    public ValueTask<Done> RemovePasskeyAsync(string id) => _accounts.RemovePasskeyAsync(new RemovePasskeyRequest { Id = id });

    public ValueTask<SessionList> ListSessionsAsync() => _accounts.ListSessionsAsync(new Nothing());

    public ValueTask<Done> EndSessionAsync(string id) => _accounts.EndSessionAsync(new EndSessionRequest { Id = id });

    public async Task SignOutAsync()
    {
        SessionList sessions = await ListSessionsAsync();
        if (sessions.Sessions.FirstOrDefault(session => session.Current) is { } current)
            await EndSessionAsync(current.Id);
        NotifySessionChanged();
    }

    public async Task SignOutEverywhereAsync()
    {
        await _accounts.SignOutEverywhereAsync(new Nothing());
        NotifySessionChanged();
    }

    public void NotifySessionChanged() => SessionChanged?.Invoke();
}
