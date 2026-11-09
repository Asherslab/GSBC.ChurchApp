using Grpc.Core;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Notifications.Client;

public sealed class AnnouncementsClient(ChannelBase channel)
{
    private readonly IAnnouncementsService _service = channel.CreateGrpcService<IAnnouncementsService>();

    public async Task<IReadOnlyList<Announcement>> ListAsync() => (await _service.ListAsync(new AnnouncementQuery())).Announcements;

    public Task<Announcement> SaveAsync(Announcement announcement) => _service.SaveAsync(announcement).AsTask();

    public Task DeleteAsync(string id) => _service.DeleteAsync(new AnnouncementRequest { Id = id }).AsTask();

    public Task SendToMeAsync(string id) => _service.SendToMeAsync(new AnnouncementRequest { Id = id }).AsTask();

    public Task<Announcement> AskToSendAsync(string id) => _service.AskToSendAsync(new AnnouncementRequest { Id = id }).AsTask();

    public Task<Announcement> SendAsync(string id, DateTime? at) => _service.SendAsync(new AnnouncementRequest { Id = id, SendAt = at }).AsTask();

    public Task<Announcement> CancelAsync(string id) => _service.CancelAsync(new AnnouncementRequest { Id = id }).AsTask();

    public Task<Announcement> RecallAsync(string id) => _service.RecallAsync(new AnnouncementRequest { Id = id }).AsTask();

    public Task<Reach> ReachAsync(AudienceMessage audience) => _service.ReachAsync(audience).AsTask();
}
