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

    public string? AppVersion { get; private set; }

    public string? AppBuild { get; private set; }

    public string? Platform { get; private set; }

    public Uri? Gateway { get; private set; }

    public int OsMajor { get; private set; }

    public string? OsVersion { get; private set; }

    public string? Model { get; private set; }

    public event Action<bool>? AppStateChanged;

    public event Action? CrashesArrived;

    public async Task InitialiseAsync()
    {
        IsNative = await js.InvokeAsync<bool>("gsbcNative.isNative");
        if (!IsNative)
            return;

        IsDevelopmentBuild = await js.InvokeAsync<bool>("gsbcNative.isDevelopmentBuild");
        SignedIn = await js.InvokeAsync<bool>("gsbcNative.isSignedIn");
        (AppVersion, AppBuild) = await js.InvokeAsync<NativeAppVersion>("gsbcNative.appVersion");
        NativeDevice device = await js.InvokeAsync<NativeDevice>("gsbcNative.device");
        (Platform, OsMajor, OsVersion, Model) = (device.Platform, device.OsMajor, device.OsVersion, device.Model);
        Gateway = Uri.TryCreate(await js.InvokeAsync<string?>("gsbcNative.gateway"), UriKind.Absolute, out Uri? gateway) ? gateway : null;
        _receiver = DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("gsbcNative.listenSession", _receiver);
        await js.InvokeVoidAsync("gsbcNative.listenAppState", _receiver);
        await js.InvokeVoidAsync("gsbcNative.listenCrashes", _receiver);
    }

    public async Task OpenLinksWithAsync(Action<string> open)
    {
        _openLink = open;
        if (_receiver is not null)
            await js.InvokeVoidAsync("gsbcNative.listenLinks", _receiver);
    }

    public ValueTask<string?> SignInWithPasskeyAsync() => js.InvokeAsync<string?>("gsbcNative.signIn");

    public ValueTask<string?> RegisterPasskeyAsync(string? ticket) => js.InvokeAsync<string?>("gsbcNative.registerPasskey", ticket);

    public ValueTask<string?> SignInWithCodeAsync(string email, string code) => js.InvokeAsync<string?>("gsbcNative.signInWithCode", email, code);

    public ValueTask ForgetSessionAsync() => IsNative ? js.InvokeVoidAsync("gsbcNative.signOut") : ValueTask.CompletedTask;

    public ValueTask<string> TakeCrashesAsync() => js.InvokeAsync<string>("gsbcNative.takeCrashes");

    public ValueTask<bool> ProveInstallAsync() => IsNative ? js.InvokeAsync<bool>("gsbcNative.proveInstall") : ValueTask.FromResult(false);

    public ValueTask<string> PushPermissionAsync() => js.InvokeAsync<string>("gsbcNative.pushPermission");

    public ValueTask<NativePushRegistration> RegisterPushAsync() => js.InvokeAsync<NativePushRegistration>("gsbcNative.registerPush");

    public ValueTask SetBadgeAsync(int count) => IsNative ? js.InvokeVoidAsync("gsbcNative.setBadge", count) : ValueTask.CompletedTask;

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

    [JSInvokable]
    public void OnAppStateChanged(bool active) => AppStateChanged?.Invoke(active);

    [JSInvokable]
    public void OnCrashesArrived() => CrashesArrived?.Invoke();

    public void Dispose() => _receiver?.Dispose();
}

public sealed record NativeAppVersion(string? Version, string? Build);

public sealed record NativeDevice(string? Platform, int OsMajor, string? OsVersion, string? Model);

public sealed record NativePushRegistration(string Permission, string? Token, string? Environment, string? AppId);

public sealed record NativeUnaryResult(int Status, string? Message, string? Response);
