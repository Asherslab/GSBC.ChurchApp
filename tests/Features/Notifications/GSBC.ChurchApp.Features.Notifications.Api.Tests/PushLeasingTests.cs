using GSBC.ChurchApp.Features.Notifications.Api.Push;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

[Integration("FOR UPDATE SKIP LOCKED leases each due push to one dispatcher, and a push left to retry is leased again once it is due")]
public sealed class PushLeasingTests : NotificationsHost
{
    [Fact]
    public async Task TwoDispatchersAtOnceSendEachPushOnce()
    {
        Guid person = Guid.CreateVersion7();
        await RegisterAsync(Guid.CreateVersion7(), person);
        for (int swap = 1; swap <= 5; swap++)
            await RaiseAsync(new Notification(TestKinds.RosterSwap, NotificationAudience.Person(person), $"Swap {swap}", ""));

        await Task.WhenAll(DeliverAsync(), DeliverAsync());

        Assert.Equal(["Swap 1", "Swap 2", "Swap 3", "Swap 4", "Swap 5"], Transport.Sent.Select(push => push.Message.Title!).Order());
    }

    [Fact]
    public async Task ADeadTokenIsDroppedAndAFailedSendIsRetried()
    {
        Guid gone = Guid.CreateVersion7(), flaky = Guid.CreateVersion7(), person = Guid.CreateVersion7();
        Holders.Everyone = [person];
        await RegisterAsync(gone, person);
        await RegisterAsync(flaky, null);
        Transport.Outcomes[Token(gone)] = new Queue<PushResult>([new PushResult(PushOutcome.DeadToken, "Unregistered")]);
        Transport.Outcomes[Token(flaky)] = new Queue<PushResult>([new PushResult(PushOutcome.Retry, "Service unavailable")]);

        await RaiseAsync(new Notification(AnnouncementNotifications.Announcement, NotificationAudience.Everyone, "Carols at 7", ""));
        await DeliverAsync();
        Assert.Equal([flaky], await ListAsync(db => db.Devices.Select(device => device.InstallId)));

        Clock.Advance(TimeSpan.FromMinutes(1));
        await DeliverAsync();
        Assert.Equal((Token(flaky), PushOutcome.Sent), (Transport.Sent[^1].Target.Token, Transport.Sent[^1].Result.Outcome));
    }
}
