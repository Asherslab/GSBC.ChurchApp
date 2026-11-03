using Grpc.Core;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Sessions;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Notifications.Client;

public sealed class PushRegistration(NativeBridge bridge, ChannelBase channel, SessionSignal session, Inbox inbox) : IClientStartupTask
{
    public const string Granted = "granted";
    public const string Denied = "denied";
    public const string Undecided = "ask";
    private const string ClaimMethod = "/gsbc.accounts.Installs/Claim";

    private readonly INotificationsService _notifications = channel.CreateGrpcService<INotificationsService>();
    private bool _signedIn;

    public string Permission { get; private set; } = "unavailable";

    public event Action? Changed;

    public async Task RunAsync()
    {
        if (!bridge.IsNative)
            return;
        _signedIn = bridge.SignedIn;
        session.Changed += OnSessionChanged;
        bridge.AppStateChanged += OnAppStateChanged;
        Permission = await bridge.PushPermissionAsync();
        if (Permission == Granted)
            _ = RegisterAsync();
        await inbox.RefreshCountAsync();
    }

    public async Task<string> TurnOnAsync()
    {
        await RegisterAsync();
        return Permission;
    }

    private async Task RegisterAsync()
    {
        try
        {
            NativePushRegistration registration = await bridge.RegisterPushAsync();
            Permission = registration.Permission;
            Changed?.Invoke();
            if (registration.Token is not { } token || !await bridge.ProveInstallAsync())
                return;
            await _notifications.RegisterDeviceAsync(new RegisterDeviceRequest
            {
                Token = token, Platform = "ios", AppId = registration.AppId ?? "", Environment = registration.Environment ?? "",
            });
        }
        catch (RpcException)
        {
        }
    }

    private void OnAppStateChanged(bool active)
    {
        if (active)
            _ = RefreshPermissionAsync();
    }

    private async Task RefreshPermissionAsync()
    {
        string permission = await bridge.PushPermissionAsync();
        if (permission == Permission)
            return;
        bool grantedNow = permission == Granted;
        Permission = permission;
        Changed?.Invoke();
        if (grantedNow)
            await RegisterAsync();
    }

    private async Task OnSessionChanged()
    {
        bool signedIn = bridge.SignedIn;
        bool signedInNow = signedIn && !_signedIn;
        _signedIn = signedIn;
        if (signedInNow && await bridge.ProveInstallAsync())
            await bridge.UnaryAsync(ClaimMethod, [], CancellationToken.None);
        if (Permission == Granted || signedInNow)
            await RegisterAsync();
    }
}
