using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

[Integration("Who gets an inbox row and a push is decided as the raiser writes them to Postgres")]
public sealed class ActorTests : NotificationsHost
{
    [Fact]
    public async Task WhoeverCausedANotificationIsNeverToldAboutIt()
    {
        Guid author = Guid.CreateVersion7(), member = Guid.CreateVersion7();
        Holders.Grant(TestKinds.ReceiveNotices, TestKinds.Music, people: [author, member], phones: []);
        await RegisterAsync(author, author);
        await RegisterAsync(member, member);

        await RaiseAsync(new Notification(TestKinds.Notice, NotificationAudience.Holding(TestKinds.ReceiveNotices, TestKinds.Music), "No practice tonight", "",
            Link: "/ministries/music/news", Subject: "ministry/music/notice/1", ScopeId: TestKinds.Music) { Actor = author });
        await DeliverAsync();

        Assert.Equal([member], await ListAsync(db => db.Inbox.Select(entry => entry.UserId)));
        Assert.Equal([Token(member)], Transport.Sent.Select(push => push.Target.Token));
    }

    [Fact]
    public async Task TryingSomethingOnYourOwnPhoneStillReachesIt()
    {
        Guid staff = Guid.CreateVersion7();
        await RegisterAsync(staff, staff);

        await RaiseAsync(new Notification(TestKinds.Notice, NotificationAudience.Person(staff), "Christmas services", "", Test: true) { Actor = staff });
        await DeliverAsync();

        Assert.Equal([staff], await ListAsync(db => db.Inbox.Select(entry => entry.UserId)));
        Assert.Single(Transport.Sent);
    }
}
