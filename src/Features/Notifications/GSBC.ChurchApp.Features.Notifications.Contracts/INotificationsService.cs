using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Notifications.Contracts;

[Service("gsbc.notifications.Notifications")]
public interface INotificationsService
{
    [SignedIn]
    ValueTask<Done> MarkReadAsync(MarkReadRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> SetPreferenceAsync(PreferenceItem request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> RegisterDeviceAsync(RegisterDeviceRequest request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> SetPhonePreferenceAsync(PreferenceItem request, CallContext context = default);
}
