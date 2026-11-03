using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Client.Analytics;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Features.Notifications.Client;

internal sealed class PushSetting(NativeBridge bridge) : IWeeklyState
{
    public Measure Measure => NotificationsMeasures.Push;

    public async ValueTask<string> CurrentAsync() => await bridge.PushPermissionAsync() switch
    {
        PushRegistration.Granted => NotificationsMeasures.On,
        PushRegistration.Denied => NotificationsMeasures.Off,
        _ => NotificationsMeasures.Undecided,
    };
}
