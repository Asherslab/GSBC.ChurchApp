using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

[Integration("Raising, reading and settling a notification write the next version of its rows to the inbox and public feeds")]
public sealed class InboxFeedTests : NotificationsHost
{
    [Fact]
    public async Task MembersGetItInTheirInboxAndPhonesFollowingTheMinistrySeeItWithoutSigningIn()
    {
        Guid member = Guid.CreateVersion7();
        Holders.Grant(TestKinds.ReceiveNotices, TestKinds.Music, people: [member], phones: [Guid.CreateVersion7()]);

        await RaiseAsync(new Notification(TestKinds.Notice, NotificationAudience.Holding(TestKinds.ReceiveNotices, TestKinds.Music), "No practice tonight", "",
            Link: "/ministries/music/news", Subject: "ministry/music/notice/1", ScopeId: TestKinds.Music) { Actor = null });

        Assert.Equal([member.ToString()], await ListAsync(db => db.FeedChanges.Where(change => change.Feed == NotificationsFeeds.Inbox).Select(change => change.ScopeId!)));
        FeedChangeRecord publicRow = Assert.Single(await ListAsync(db => db.FeedChanges.Where(change => change.Feed == NotificationsFeeds.Public)));
        Assert.Equal(TestKinds.Music, publicRow.GroupKey);
    }

    [Fact]
    public async Task ReadingAnItemReachesThePersonsOtherDevices()
    {
        Guid person = Guid.CreateVersion7();
        await RaiseAsync(new Notification(TestKinds.Notice, NotificationAudience.Person(person), "Choir at 6", "") { Actor = null });
        InboxEntry entry = Assert.Single(await ListAsync(db => db.Inbox));

        await WithAsync<NotificationsService, Done>(service => service.MarkReadAsync(new MarkReadRequest { Ids = [entry.Id.ToString()] }, CallAs(person)).AsTask());

        FeedChangeRecord latest = await LatestInboxRowAsync();
        Assert.Equal((2L, person.ToString()), (latest.Version, latest.ScopeId));
        Assert.NotNull(Item(latest).ReadAt);
    }

    [Fact]
    public async Task SomethingWaitingLeavesWaitingOnceItIsDoneWherever()
    {
        Guid person = Guid.CreateVersion7();
        await RaiseAsync(new Notification(TestKinds.RosterSwap, NotificationAudience.Person(person), "Can you swap Sunday?", "", Link: "/ministries/music/roster",
            Subject: "roster/9", Action: "See the roster") { Actor = null });
        Assert.True(Item(await LatestInboxRowAsync()).Waiting);

        await WithAsync<NotificationRaiser, bool>(async raiser =>
        {
            await raiser.SettleAsync("roster/9", Cancel);
            return true;
        });

        FeedChangeRecord settled = await LatestInboxRowAsync();
        Assert.Equal(2L, settled.Version);
        Assert.False(Item(settled).Waiting);
    }
}
