using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Prayer.Contracts;

[Service("gsbc.prayer.Prayer")]
public interface IPrayerService
{
    [AppInstall]
    ValueTask<Done> SendAsync(PrayerRequestSend request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> PrayAsync(PrayMark request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> AnsweredAsync(AnsweredSave request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> TakeDownAsync(RequestRef request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> DeleteAsync(RequestRef request, CallContext context = default);
}
