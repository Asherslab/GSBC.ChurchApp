using Grpc.Core;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

[Integration("Digests collect their items in Postgres and the sender claims the due ones there")]
public sealed class DigestTests : NotificationsHost
{
    private readonly Guid _owner = Guid.CreateVersion7();

    [Fact]
    public async Task ADigestCountsWhatIsNewSinceTheLastOneAndSendsNothingWhenNothingIs()
    {
        DateTime made = Clock.UtcNow;
        await PrayedAsync(made, by: Guid.CreateVersion7());
        await PrayedAsync(made, by: null);

        await At(made.AddMinutes(15));
        await At(made.AddMinutes(30));
        Clock.Advance(TimeSpan.FromMinutes(10));
        await PrayedAsync(made, by: null);
        await At(made.AddHours(1));
        Clock.Advance(TimeSpan.FromMinutes(30));
        await PrayedAsync(made, by: null);
        await At(made.AddHours(1.98));
        await At(made.AddHours(2));

        Assert.Equal(["2 prayers for your request", "Someone prayed for your request", "Someone prayed for your request"], await TitlesAsync(_owner));
    }

    [Fact]
    public async Task PrayingForYourOwnRequestIsNotCountedForYou()
    {
        DateTime made = Clock.UtcNow;
        await PrayedAsync(made, by: _owner);
        await At(made.AddMinutes(15));
        await PrayedAsync(made, by: _owner);
        await PrayedAsync(made, by: Guid.CreateVersion7());
        await At(made.AddMinutes(30));

        Assert.Equal(["Someone prayed for your request"], await TitlesAsync(_owner));
    }

    [Fact]
    public async Task ADailyDigestGoesAtMiddayChurchTimeCountingWhatEachPersonDidNotDoThemselves()
    {
        Guid poster = Guid.CreateVersion7(), reader = Guid.CreateVersion7();
        Holders.Grant(TestKinds.ReceiveNotices, TestKinds.Music, people: [poster, reader], phones: []);
        DateTime morning = Clock.UtcNow;
        await NoticeAsync(by: poster);
        Clock.Advance(TimeSpan.FromMinutes(30));
        await NoticeAsync(by: null);

        await At(morning.AddHours(1.9));
        await At(morning.AddHours(2));
        await At(morning.AddDays(1).AddHours(2));

        Assert.Equal(["A new notice today"], await TitlesAsync(poster));
        Assert.Equal(["2 new notices today"], await TitlesAsync(reader));
    }

    [Fact]
    public async Task OnlyThoseAllowedChangeWhenADigestGoesAndTheNextOneFollowsIt()
    {
        Guid pastor = Guid.CreateVersion7(), member = Guid.CreateVersion7();
        Holders.Grant(TestKinds.ChangePrayerSettings, null, people: [pastor], phones: []);
        DigestScheduleChange everyFiveMinutes = new() { Kind = TestKinds.Prayed.Key, AfterMinutes = [5] };

        await Assert.ThrowsAsync<RpcException>(() => WithAsync<DigestSchedulesService, DigestScheduleMessage>(service => service.ChangeAsync(everyFiveMinutes, CallAs(member)).AsTask()));
        DigestScheduleMessage changed = await WithAsync<DigestSchedulesService, DigestScheduleMessage>(service => service.ChangeAsync(everyFiveMinutes, CallAs(pastor)).AsTask());
        DateTime made = Clock.UtcNow;
        await PrayedAsync(made, by: null);
        await At(made.AddMinutes(5));

        Assert.Equal([5], changed.AfterMinutes);
        Assert.Equal(((int?)null, true), (changed.DailyAtMinute, changed.MayChange));
        Assert.Equal(["Someone prayed for your request"], await TitlesAsync(_owner));
    }

    private Task PrayedAsync(DateTime made, Guid? by) =>
        CollectAsync(new NotificationDigestItem(TestKinds.Prayed, NotificationAudience.Person(_owner), "request-1", "/prayer/yours/request-1", made) { Actor = by });

    private Task NoticeAsync(Guid? by) =>
        CollectAsync(new NotificationDigestItem(TestKinds.NewNotices, NotificationAudience.Holding(TestKinds.ReceiveNotices, TestKinds.Music), "music",
            "/ministries/music/news", ScopeId: TestKinds.Music) { Actor = by });

    private async Task At(DateTime when)
    {
        Clock.Advance(when - Clock.UtcNow);
        await SendDigestsAsync();
    }

    private Task<List<string>> TitlesAsync(Guid person) =>
        ListAsync(db => db.Inbox.Where(entry => entry.UserId == person).OrderBy(entry => entry.Id).Select(entry => entry.Notification!.Title));
}
