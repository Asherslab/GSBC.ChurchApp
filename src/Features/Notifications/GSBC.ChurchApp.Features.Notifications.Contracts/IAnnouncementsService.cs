using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Notifications.Contracts;

[Service("gsbc.notifications.Announcements")]
public interface IAnnouncementsService
{
    [Requires(NotificationsPermissions.WriteNode)]
    ValueTask<AnnouncementList> ListAsync(AnnouncementQuery request, CallContext context = default);

    [Requires(NotificationsPermissions.WriteNode)]
    ValueTask<Announcement> SaveAsync(Announcement request, CallContext context = default);

    [Requires(NotificationsPermissions.WriteNode)]
    ValueTask<Done> DeleteAsync(AnnouncementRequest request, CallContext context = default);

    [Requires(NotificationsPermissions.WriteNode)]
    ValueTask<Done> SendToMeAsync(AnnouncementRequest request, CallContext context = default);

    [Requires(NotificationsPermissions.WriteNode)]
    ValueTask<Announcement> AskToSendAsync(AnnouncementRequest request, CallContext context = default);

    [Requires(NotificationsPermissions.WriteNode)]
    ValueTask<Reach> ReachAsync(AudienceMessage request, CallContext context = default);

    [Requires(NotificationsPermissions.SendNode)]
    ValueTask<Announcement> SendAsync(AnnouncementRequest request, CallContext context = default);

    [Requires(NotificationsPermissions.SendNode)]
    ValueTask<Announcement> CancelAsync(AnnouncementRequest request, CallContext context = default);
}
