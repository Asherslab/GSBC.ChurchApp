using GSBC.ChurchApp.Features.Accounts.Client;
using GSBC.ChurchApp.Platform.Client.Native;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Features.Accounts.App;

internal sealed class NativePasskeyCeremonies(NativeBridge bridge, IJSRuntime js) : IPasskeyCeremonies
{
    private readonly BrowserPasskeyCeremonies _browser = new(js);

    public ValueTask<bool> IsSupportedAsync() => bridge.IsNative ? ValueTask.FromResult(true) : _browser.IsSupportedAsync();

    public async ValueTask<CeremonyResult> RegisterAsync(string? ticket) =>
        bridge.IsNative ? Outcome(await bridge.RegisterPasskeyAsync(ticket)) : await _browser.RegisterAsync(ticket);

    public async ValueTask<CeremonyResult> SignInAsync() =>
        bridge.IsNative ? Outcome(await bridge.SignInWithPasskeyAsync()) : await _browser.SignInAsync();

    private static CeremonyResult Outcome(string? error) => new(error is null, error);
}
