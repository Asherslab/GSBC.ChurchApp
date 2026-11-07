using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Api.Push;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

public sealed class PushRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PushesHeldPastAKindsPaceArriveAsOneSummaryWithTheUnreadCount()
    {
        PushMessage summary = PushRules.Message([Held(4, "Notice 4"), Held(5, "Notice 5")], badge: 5);

        Assert.Equal(("2 new · Notice 5", 5), (summary.Body, summary.Badge));
        Assert.Equal(PushRules.InboxLink, summary.Link);
    }

    [Fact]
    public void AFailedSendIsTriedAgainAfterABackoffUntilAnHourHasPassed()
    {
        Delivery recent = Held(1, "Carols at 7");
        Delivery old = Held(2, "Carols at 7");
        old.CreatedAt = Now.AddHours(-1);

        PushRules.Record([recent, old], new PushResult(PushOutcome.Retry, "Service unavailable"), Now);

        Assert.Equal((1, Now.AddMinutes(1), (DateTime?)null), (recent.Attempts, recent.DueAt, recent.GaveUpAt));
        Assert.Equal(Now, old.GaveUpAt);
    }

    private static Delivery Held(long id, string title) => new()
    {
        Id = id,
        Kind = DeliveryKind.Visible,
        Summarised = true,
        CreatedAt = Now,
        Notification = new NotificationRecord { Kind = TestKinds.Notice.Key, Area = TestKinds.Notice.Area, Title = title, ScopeId = TestKinds.Music },
    };
}
