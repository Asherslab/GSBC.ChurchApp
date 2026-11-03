using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.Sessions;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Accounts.Client;

public sealed class AccountsClient(ChannelBase channel, SessionSignal signal, IDeviceSession device)
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

    public ValueTask<ToolConnection> ConnectToolAsync(string name) => _accounts.ConnectToolAsync(new ConnectToolRequest { Name = name });

    public ValueTask<Done> EndSessionAsync(string id) => _accounts.EndSessionAsync(new EndSessionRequest { Id = id });

    public Task SignOutAsync() => ForgettingThisDeviceAsync(async () =>
    {
        SessionList sessions = await ListSessionsAsync();
        if (sessions.Sessions.FirstOrDefault(session => session.Current) is { } current)
            await EndSessionAsync(current.Id);
    });

    public Task SignOutEverywhereAsync() => ForgettingThisDeviceAsync(async () => await _accounts.SignOutEverywhereAsync(new Nothing()));

    private async Task ForgettingThisDeviceAsync(Func<Task> endOnServer)
    {
        try
        {
            await endOnServer();
        }
        finally
        {
            await device.ForgetAsync();
            await NotifySessionChangedAsync();
        }
    }

    public ValueTask<PeopleList> SearchPeopleAsync(string query) => _accounts.SearchPeopleAsync(new SearchPeopleRequest { Query = query });

    public ValueTask<PeopleList> GetPeopleAsync(IEnumerable<string> ids) => _accounts.GetPeopleAsync(new GetPeopleRequest { Ids = ids.ToList() });

    public ValueTask<Done> InviteAsync(string name, string email) => _accounts.InviteAsync(new InviteRequest { DisplayName = name, Email = email });

    public ValueTask<CodeReply> IssueRecoveryCodeAsync(string userId) => _accounts.IssueRecoveryCodeAsync(new IssueRecoveryCodeRequest { UserId = userId });

    public ValueTask<ReviewAccess> GetReviewAccessAsync() => _accounts.GetReviewAccessAsync(new Nothing());

    public ValueTask<Done> CreateDemoAccountAsync(string name, string email) =>
        _accounts.CreateDemoAccountAsync(new CreateDemoAccountRequest { DisplayName = name, Email = email });

    public ValueTask<CodeReply> CreateReviewCodeAsync(string userId, string label, int days) =>
        _accounts.CreateReviewCodeAsync(new CreateReviewCodeRequest { UserId = userId, Label = label, Days = days });

    public ValueTask<Done> TurnOffReviewCodeAsync(string id) => _accounts.TurnOffReviewCodeAsync(new TurnOffReviewCodeRequest { Id = id });

    public ValueTask<Done> SetDisabledAsync(string userId, bool disabled) =>
        _accounts.SetDisabledAsync(new SetDisabledRequest { UserId = userId, Disabled = disabled });

    public ValueTask<AuditList> ListAuditAsync(string? userId) => _accounts.ListAuditAsync(new AuditQuery { UserId = userId });

    public async Task NotifySessionChangedAsync()
    {
        SessionChanged?.Invoke();
        await signal.RaiseAsync();
    }
}
