using GSBC.ChurchApp.Platform.Client.Sessions;

namespace GSBC.ChurchApp.Platform.Client.Native;

internal sealed class NativeDeviceSession(NativeBridge bridge) : IDeviceSession
{
    public ValueTask ForgetAsync() => bridge.ForgetSessionAsync();
}
