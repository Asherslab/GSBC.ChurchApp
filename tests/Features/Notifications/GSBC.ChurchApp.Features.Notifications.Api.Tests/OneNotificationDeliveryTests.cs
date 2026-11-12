using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

[Integration("The choice between related notifications decides which inbox rows and pushes the raiser writes to Postgres")]
public sealed class OneNotificationDeliveryTests : NotificationsHost
{
    [Fact]
    public async Task SomeoneFollowingASeriesAndEverySermonGetsOneInboxRowAndOnePush()
    {
        Guid both = Guid.CreateVersion7(), everySermon = Guid.CreateVersion7();
        Holders.Grant(TestKinds.HearSeries, "summer-fit", people: [both], phones: []);
        Holders.Grant(TestKinds.HearNew, null, people: [both, everySermon], phones: []);
        await RegisterAsync(Guid.CreateVersion7(), both);
        await RegisterAsync(Guid.CreateVersion7(), everySermon);

        await RaiseNewSermonAsync();
        await DeliverAsync();

        List<(Guid UserId, string Title)> inbox = (await ListAsync(db => db.Inbox.Select(entry => new { entry.UserId, entry.Notification!.Title })))
            .Select(entry => (entry.UserId, entry.Title)).ToList();
        Assert.Equal([(both, "New in Summer Fit: Drop The Weight"), (everySermon, "New sermon: Drop The Weight")], inbox.OrderBy(entry => entry.Title));
        Assert.Equal(["New in Summer Fit: Drop The Weight", "New sermon: Drop The Weight"], Transport.Sent.Select(push => push.Message.Title!).Order());
    }

    [Fact]
    public async Task SomeoneWhoTurnedOffEveryReasonGetsNothingInTheInboxOrOnTheirPhone()
    {
        Guid person = Guid.CreateVersion7();
        Holders.Grant(TestKinds.HearSeries, "summer-fit", people: [person], phones: []);
        Holders.Grant(TestKinds.HearNew, null, people: [person], phones: []);
        await RegisterAsync(Guid.CreateVersion7(), person);
        foreach (NotificationKind kind in new[] { TestKinds.NewInSeries, TestKinds.NewSermon })
            await WithAsync<NotificationsService, Done>(service => service.SetPreferenceAsync(new PreferenceItem { Kind = kind.Key, Push = false }, CallAs(person)).AsTask());

        await RaiseNewSermonAsync();
        await DeliverAsync();

        Assert.Empty(await ListAsync(db => db.Inbox));
        Assert.Empty(Transport.Sent);
    }

    private Task<bool> RaiseNewSermonAsync() =>
        WithAsync<NotificationRaiser, bool>(async raiser =>
        {
            await raiser.RaiseOneOfAsync(
            [
                new Notification(TestKinds.NewInSeries, NotificationAudience.Holding(TestKinds.HearSeries, "summer-fit"), "New in Summer Fit: Drop The Weight", "",
                    Link: "/sermon/1", Subject: "sermon/1", ScopeId: "summer-fit") { Actor = null },
                new Notification(TestKinds.NewSermon, NotificationAudience.Holding(TestKinds.HearNew), "New sermon: Drop The Weight", "",
                    Link: "/sermon/1", Subject: "sermon/1", ScopeId: "sermons") { Actor = null },
            ], Cancel);
            return true;
        });
}
