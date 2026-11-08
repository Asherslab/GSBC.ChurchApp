using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

public sealed class NotificationRulesTests
{
    private static readonly DateTime Morning = ChurchTime.ToUtc(new DateTime(2026, 11, 8, 10, 0, 0));

    [Fact]
    public void ANotificationAboutAThingMustOpenThatThing()
    {
        Assert.Throws<ArgumentException>(() => Validated(new Notification(TestKinds.Notice, NotificationAudience.Everyone, "Room change", "",
            Subject: "ministry/music/notice/1")));
        Assert.Throws<ArgumentException>(() => Validated(new Notification(TestKinds.Notice, NotificationAudience.Everyone, "Room change", "",
            Link: "https://example.com", Subject: "ministry/music/notice/1")));

        Validated(new Notification(TestKinds.Notice, NotificationAudience.Everyone, "Room change", "", Link: "/ministries/music/news", Subject: "ministry/music/notice/1"));
    }

    [Fact]
    public async Task FollowersAndMembersHearFromTheirMinistryAndAPhoneSeesItWithoutSigningIn()
    {
        Guid member = Guid.CreateVersion7(), outsider = Guid.CreateVersion7(), followingPhone = Guid.CreateVersion7(), otherPhone = Guid.CreateVersion7();
        Holders holders = new() { Everyone = [member, outsider] };
        holders.Grant(TestKinds.ReceiveNotices, TestKinds.Music, people: [member], phones: [followingPhone]);
        Device[] devices = [Phone(followingPhone, null), Phone(otherPhone, null), Phone(Guid.CreateVersion7(), member), Phone(Guid.CreateVersion7(), outsider)];

        Recipients recipients = await NotificationRules.RecipientsAsync(new Notification(TestKinds.Notice,
            NotificationAudience.Holding(TestKinds.ReceiveNotices, TestKinds.Music), "No practice tonight", ""), holders, holders, TestContext.Current.CancellationToken);

        Assert.Equal([member], recipients.People);
        Assert.True(recipients.Public);
        Assert.Equal([devices[0], devices[2]], devices.Where(recipients.Devices().Compile()));
    }

    [Fact]
    public void TurningAKindOffStopsItButSafetyNoticesAlwaysArrive()
    {
        Guid person = Guid.CreateVersion7();
        Device phone = Phone(Guid.CreateVersion7(), person);
        Preference[] preferences =
        [
            new() { UserId = person, Kind = TestKinds.ChatMessage.Key, ScopeId = TestKinds.Music, Push = false },
            new() { UserId = person, Kind = TestKinds.NewPasskey.Key, Push = false },
        ];

        Assert.False(NotificationRules.Wanted(TestKinds.ChatMessage, TestKinds.Music, phone, preferences));
        Assert.True(NotificationRules.Wanted(TestKinds.NewPasskey, null, phone, preferences));
    }

    [Fact]
    public void OvernightPushesWaitForMorningUnlessUrgent()
    {
        DateTime lateAtNight = ChurchTime.ToUtc(new DateTime(2026, 11, 8, 22, 30, 0));

        Assert.Equal(ChurchTime.ToUtc(new DateTime(2026, 11, 9, 7, 0, 0)), DueAt(new Notification(TestKinds.Notice, NotificationAudience.Everyone, "Bring a plate on Sunday", ""), lateAtNight));
        Assert.Equal(lateAtNight, DueAt(new Notification(TestKinds.Notice, NotificationAudience.Everyone, "Tonight's service is cancelled", "", Urgent: true), lateAtNight));
    }

    [Fact]
    public void PastAKindsPaceTheRestWaitToBeSummarised()
    {
        Notification notice = new(TestKinds.Notice, NotificationAudience.Everyone, "Notice", "", ScopeId: TestKinds.Music);

        Assert.Equal((false, Morning), NotificationRules.Scheduled(TestKinds.Notice, notice, recentPushes: 2, Morning));
        Assert.Equal((true, Morning.AddMinutes(10)), NotificationRules.Scheduled(TestKinds.Notice, notice, recentPushes: 3, Morning));
    }

    private static void Validated(Notification notification) => NotificationRules.Validated(TestKinds.Catalog, notification);

    private static DateTime DueAt(Notification notification, DateTime now) => NotificationRules.Scheduled(notification.Kind, notification, 0, now).DueAt;

    private static Device Phone(Guid installId, Guid? userId) =>
        new() { Id = Guid.CreateVersion7(), InstallId = installId, UserId = userId, Platform = "ios", AppId = "au.com.baptist.app.dev", Environment = "", Token = "" };
}
