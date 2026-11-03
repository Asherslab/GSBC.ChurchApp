using GSBC.ChurchApp.Features.Accounts.Client;
using GSBC.ChurchApp.Platform.Client.Native;

namespace GSBC.ChurchApp.Features.Accounts.App;

internal sealed class NativeSignInCodes(NativeBridge bridge) : ISignInCodes
{
    public bool IsAvailable => bridge.IsNative;

    public async ValueTask<CeremonyResult> SignInAsync(string email, string code) =>
        await bridge.SignInWithCodeAsync(email, code) is { } error ? new(false, error) : new(true, null);
}
