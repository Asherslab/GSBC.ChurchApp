using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Events.Contracts;

[Service("gsbc.events.Digest")]
public interface IDigestService
{
    [Requires(EventsPermissions.DigestNode)]
    ValueTask<DigestDraft> OpenAsync(DigestQuery request, CallContext context = default);

    [Requires(EventsPermissions.DigestNode)]
    ValueTask<DigestDraft> SaveAsync(DigestSave request, CallContext context = default);

    [Requires(EventsPermissions.DigestNode)]
    ValueTask<DigestDraft> SendToAppAsync(DigestRef request, CallContext context = default);

    [Requires(EventsPermissions.DigestNode)]
    ValueTask<Done> SendToMyPhoneAsync(DigestRef request, CallContext context = default);
}
