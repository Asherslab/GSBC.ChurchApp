using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Features.Accounts.Client;

public sealed record CeremonyResult(bool Ok, string? Error);

public sealed class PasskeyCeremonies(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./_content/GSBC.ChurchApp.Features.Accounts.Client/passkeys.js";
    private IJSObjectReference? _module;

    public async ValueTask<bool> IsSupportedAsync() => await (await ModuleAsync()).InvokeAsync<bool>("isSupported");

    public async ValueTask<CeremonyResult> RegisterAsync(string? ticket) =>
        await (await ModuleAsync()).InvokeAsync<CeremonyResult>("register", ticket);

    public async ValueTask<CeremonyResult> SignInAsync() => await (await ModuleAsync()).InvokeAsync<CeremonyResult>("signIn");

    private async ValueTask<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
            await _module.DisposeAsync();
    }
}
