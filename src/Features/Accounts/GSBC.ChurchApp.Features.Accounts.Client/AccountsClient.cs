using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.Sessions;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Accounts.Client;

public sealed class AccountsClient(ChannelBase channel, SessionSignal signal, IDeviceSession device)
{
    private readonly IAccountsService _accounts = channel.CreateGrpcService<IAccountsService>();

    public event Action? SessionChanged;

    public ValueTask<TicketReply> StartSignUpAsync(string name, string email) =>
        _accounts.StartSignUpAsync(new StartSignUpRequest { DisplayName = name, Email = email });

    public ValueTask<TicketReply> RedeemAsync(string code) => _accounts.RedeemAsync(new RedeemRequest { Code = code });

    public ValueTask<Done> RequestRecoveryAsync(string email) => _accounts.RequestRecoveryAsync(new RequestRecoveryRequest { Email = email });

    public ValueTask<Done> UpdateNameAsync(string name) => _accounts.UpdateNameAsync(new UpdateNameRequest { DisplayName = name });

    public ValueTask<Done> ConfirmEmailAsync(string code) => _accounts.ConfirmEmailAsync(new ConfirmEmailRequest { Code = code });

    public ValueTask<Done> ResendConfirmationAsync() => _accounts.ResendConfirmationAsync(new Nothing());

    public ValueTask<Done> RemovePasskeyAsync(string id) => _accounts.RemovePasskeyAsync(new RemovePasskeyRequest { Id = id });

    public ValueTask<Done> EndSessionAsync(string id) => _accounts.EndSessionAsync(new EndSessionRequest { Id = id });

    public Task SignOutAsync() => ForgettingThisDeviceAsync(async () => await EndSessionAsync(""));

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

    public async Task DeleteMyAccountAsync()
    {
        await _accounts.DeleteMyAccountAsync(new Nothing());
        await device.ForgetAsync();
        await NotifySessionChangedAsync();
    }

    public async Task NotifySessionChangedAsync()
    {
        SessionChanged?.Invoke();
        await signal.RaiseAsync();
    }
}
