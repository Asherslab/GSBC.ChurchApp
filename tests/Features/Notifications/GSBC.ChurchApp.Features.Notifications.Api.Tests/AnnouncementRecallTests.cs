using Grpc.Core;
using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

[Integration("Recalling a sent announcement removes its inbox and public rows, read counts and pending pushes in Postgres")]
public sealed class AnnouncementRecallTests : NotificationsHost
{
    private readonly Guid _sender = Guid.CreateVersion7();
    private readonly Guid _reader = Guid.CreateVersion7();

    [Fact]
    public async Task ARecalledAnnouncementLeavesEveryInboxAndEveryPhoneOnItsNextSync()
    {
        Holders.Everyone = [_reader];
        Announcement sent = await SendAsync(await DraftAsync("Carols on Friday"));

        await RecallAsync(sent.Id);

        Assert.Empty(await ListAsync(db => db.Inbox));
        Assert.All(await LatestRowsAsync(), row => Assert.True(row.Deleted));
        Assert.Equal([NotificationsFeeds.Inbox, NotificationsFeeds.Public], (await LatestRowsAsync()).Select(row => row.Feed).Order());
    }

    [Fact]
    public async Task UnreadCountsAndTheBadgeNoLongerCountIt()
    {
        Holders.Everyone = [_reader];
        await RegisterAsync(Guid.CreateVersion7(), _reader);
        Announcement sent = await SendAsync(await DraftAsync("Carols on Friday"));
        await DeliverAsync();

        await RecallAsync(sent.Id);
        await RaiseAsync(new Notification(TestKinds.Notice, NotificationAudience.Person(_reader), "Choir at 6", "") { Actor = null });
        await DeliverAsync();

        Assert.Equal(1, Transport.Sent.Last(push => !push.Message.Background).Message.Badge);
    }

    [Fact]
    public async Task APushHeldForQuietHoursIsNeverSent()
    {
        Clock.Advance(TimeSpan.FromHours(12));
        Holders.Everyone = [_reader];
        await RegisterAsync(Guid.CreateVersion7(), _reader);
        Announcement sent = await SendAsync(await DraftAsync("Carols on Friday"));

        await RecallAsync(sent.Id);
        Clock.Advance(TimeSpan.FromHours(9.5));
        await DeliverAsync();

        Assert.DoesNotContain(Transport.Sent, push => !push.Message.Background);
    }

    [Fact]
    public async Task TheHistoryKeepsItMarkedWithWhoRecalledItAndHowManyHadReadIt()
    {
        Holders.Everyone = [_reader];
        Holders.Names[_sender] = "Ruth Office";
        Announcement sent = await SendAsync(await DraftAsync("Carols on Friday"));
        await WithAsync<NotificationsService, Done>(service => service.MarkReadAsync(new MarkReadRequest { All = true }, CallAs(_reader)).AsTask());

        await RecallAsync(sent.Id);

        Announcement recalled = Assert.Single((await WithAsync<AnnouncementsService, AnnouncementList>(service =>
            service.ListAsync(new AnnouncementQuery(), CallAs(_sender)).AsTask())).Announcements);
        Assert.Equal(("Recalled", "Ruth Office", 1, "Carols on Friday"), (recalled.State, recalled.RecalledBy, recalled.ReadBy, recalled.Title));
    }

    [Fact]
    public async Task ARecalledAnnouncementCantBeSentOrChangedAsItIs()
    {
        Announcement sent = await SendAsync(await DraftAsync("Carols on Friday"));
        await RecallAsync(sent.Id);

        RpcException resend = await Assert.ThrowsAsync<RpcException>(() => SendAsync(sent));
        RpcException change = await Assert.ThrowsAsync<RpcException>(() => WithAsync<AnnouncementsService, Announcement>(service =>
            service.SaveAsync(sent, CallAs(_sender)).AsTask()));

        Assert.Equal([StatusCode.FailedPrecondition, StatusCode.FailedPrecondition], [resend.StatusCode, change.StatusCode]);
    }

    [Fact]
    public async Task OnlyASentAnnouncementCanBeRecalled()
    {
        Announcement draft = await DraftAsync("Carols on Friday");

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => RecallAsync(draft.Id));

        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
    }

    private Task<Announcement> DraftAsync(string title) =>
        WithAsync<AnnouncementsService, Announcement>(service => service.SaveAsync(
            new Announcement { Title = title, Body = "7pm in the hall", Audience = new AudienceMessage { Kind = AnnouncementsService.Everyone } }, CallAs(_sender)).AsTask());

    private Task<Announcement> SendAsync(Announcement draft) =>
        WithAsync<AnnouncementsService, Announcement>(service => service.SendAsync(new AnnouncementRequest { Id = draft.Id }, CallAs(_sender)).AsTask());

    private Task<Announcement> RecallAsync(string id) =>
        WithAsync<AnnouncementsService, Announcement>(service => service.RecallAsync(new AnnouncementRequest { Id = id }, CallAs(_sender)).AsTask());

    private async Task<List<Platform.Data.FeedChangeRecord>> LatestRowsAsync() =>
        (await ListAsync(db => db.FeedChanges.Where(change => change.Feed == NotificationsFeeds.Inbox || change.Feed == NotificationsFeeds.Public)))
        .GroupBy(change => (change.Feed, change.RecordId))
        .Select(rows => rows.MaxBy(row => row.Version)!)
        .ToList();
}
