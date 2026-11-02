using GSBC.ChurchApp.Platform.Client.Sessions;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Native;

public sealed class NativeBridge(IJSRuntime js, SessionSignal session) : IDisposable
{
    private DotNetObjectReference<NativeBridge>? _receiver;
    private Action<string>? _openLink;

    public bool IsNative { get; private set; }

    public bool IsDevelopmentBuild { get; private set; }

    public bool SignedIn { get; private set; }

    public async Task InitialiseAsync()
    {
        IsNative = await js.InvokeAsync<bool>("gsbcNative.isNative");
        if (!IsNative)
            return;

        IsDevelopmentBuild = await js.InvokeAsync<bool>("gsbcNative.isDevelopmentBuild");
        SignedIn = await js.InvokeAsync<bool>("gsbcNative.isSignedIn");
        _receiver = DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("gsbcNative.listenSession", _receiver);
    }

    public async Task OpenLinksWithAsync(Action<string> open)
    {
        _openLink = open;
        if (_receiver is not null)
            await js.InvokeVoidAsync("gsbcNative.listenLinks", _receiver);
    }

    public ValueTask<string?> SignInWithPasskeyAsync() => js.InvokeAsync<string?>("gsbcNative.signIn");

    public ValueTask<string?> RegisterPasskeyAsync(string? ticket) => js.InvokeAsync<string?>("gsbcNative.registerPasskey", ticket);

    public ValueTask ForgetSessionAsync() => IsNative ? js.InvokeVoidAsync("gsbcNative.signOut") : ValueTask.CompletedTask;

    public ValueTask<NativeUnaryResult> UnaryAsync(string method, byte[] request, CancellationToken cancellationToken) =>
        js.InvokeAsync<NativeUnaryResult>("gsbcNative.unary", cancellationToken, method, Convert.ToBase64String(request));

    [JSInvokable]
    public async Task OnSessionChanged(bool signedIn)
    {
        SignedIn = signedIn;
        await session.RaiseAsync();
    }

    [JSInvokable]
    public void OnLinkOpened(string path) => _openLink?.Invoke(path);

    public void Dispose() => _receiver?.Dispose();
}

public sealed record NativeUnaryResult(int Status, string? Message, string? Response);
