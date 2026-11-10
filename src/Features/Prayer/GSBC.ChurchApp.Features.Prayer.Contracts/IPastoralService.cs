using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Prayer.Contracts;

[Service("gsbc.prayer.Pastoral")]
public interface IPastoralService
{
    [Requires(PrayerPermissions.WallNode)]
    ValueTask<Inbox> InboxAsync(InboxRequest request, CallContext context = default);

    [Requires(PrayerPermissions.WallNode)]
    ValueTask<RequestDetails> RequestAsync(RequestRef request, CallContext context = default);

    [Requires(PrayerPermissions.ReadNode)]
    ValueTask<Done> CareAsync(CareChange request, CallContext context = default);

    [Requires(PrayerPermissions.ReadNode)]
    ValueTask<Done> AddNoteAsync(NoteSave request, CallContext context = default);

    [Requires(PrayerPermissions.ReadNode)]
    ValueTask<Done> DeleteNoteAsync(NoteRef request, CallContext context = default);

    [Requires(PrayerPermissions.WallNode)]
    ValueTask<Done> ShowOnWallAsync(WallSave request, CallContext context = default);

    [Requires(PrayerPermissions.WallNode)]
    ValueTask<Done> KeepPrivateAsync(RequestRef request, CallContext context = default);

    [Requires(PrayerPermissions.ReadNode)]
    ValueTask<Done> DeleteAsync(RequestRef request, CallContext context = default);
}
