using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.Analytics;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Analytics;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

public sealed class UsageTests : IAsyncLifetime
{
    private const string BibleTile = "3f1c9a52-6a1b-4a8e-9c1e-0d6c1c6f1a01";
    private static readonly string TileTapped = AnalyticsHost.Measures.TileTapped.Key;

    private readonly AnalyticsHost _host = new();

    public async ValueTask InitializeAsync() => await _host.StartAsync(new TileNames());

    [Fact]
    public void APhoneCountsItselfOnceADayWeekAndMonthAndSaysWhenItCameBack()
    {
        OpeningMemory phone = new();
        DateOnly monday = new(2026, 11, 2);

        Assert.Equal([AnalyticsMeasures.Installed, AnalyticsMeasures.ActiveDay, AnalyticsMeasures.ActiveWeek, AnalyticsMeasures.ActiveMonth], Open(phone, monday));
        Assert.Empty(Open(phone, monday));
        Assert.Equal([AnalyticsMeasures.ActiveDay], Open(phone, monday.AddDays(2)));
        Assert.Equal([AnalyticsMeasures.ActiveDay, AnalyticsMeasures.ActiveWeek, AnalyticsMeasures.Returning], Open(phone, monday.AddDays(7)));
        Assert.Equal([AnalyticsMeasures.ActiveDay, AnalyticsMeasures.ActiveWeek, AnalyticsMeasures.ActiveMonth], Open(phone, monday.AddDays(35)));
    }

    [Fact]
    public async Task OnlyDeclaredMeasuresAreKeptAndAPhoneCanOnlyCountItselfOnce()
    {
        int today = _host.Today.DayNumber;
        UsageReport report = Report(
            new UsageCount { Day = today, Measure = AnalyticsMeasures.ActiveWeek.Key, Group = UsageGroup.SignedIn, Count = 40 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.ActiveWeek.Key, Group = UsageGroup.SignedIn, Count = 1 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.Installed.Key, Group = UsageGroup.OnATeam, Count = 1 },
            new UsageCount { Day = today, Measure = "prayer.request.viewed", Subject = "42", Count = 1 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.Installed.Key, Subject = "sam@example.org", Count = 1 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.ActiveDay.Key, Count = 1 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.ActiveDay.Key, Count = 1 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.ActiveDay.Key, Count = 1 },
            new UsageCount { Day = today, Measure = TileTapped, Subject = "Sam's prayer for her mum", Count = 3 });

        await AcceptAsync(report, "phone-a-1");
        await AcceptAsync(report, "phone-a-1");

        List<DailyCountRecord> stored = await _host.WithAsync<AnalyticsDbContext, List<DailyCountRecord>>(db => db.DailyCounts.ToListAsync(AnalyticsHost.Cancellation));
        Assert.Equal(3, stored.Count);
        Assert.All(stored, count => Assert.Equal(1, count.Count));
        Assert.All(stored, count => Assert.Equal("", count.Subject));
        Assert.Equal(UsageGroup.Everyone, Assert.Single(stored, count => count.Measure == AnalyticsMeasures.Installed.Key).Group);
    }

    [Fact]
    public async Task CountsBelowFiveNeverLeaveTheServerAsNumbers()
    {
        await PhonesOpenAsync(3, UsageGroup.NotSignedIn);
        await PhonesOpenAsync(9, UsageGroup.SignedIn);
        await TapAsync(BibleTile, times: 2);

        InsightsSummary summary = await SummaryAsync();

        Assert.Equal(12, summary.ThisWeek!.Value);
        GroupTally notSignedIn = Assert.Single(summary.Groups, group => group.Group == UsageGroup.NotSignedIn);
        Assert.True(notSignedIn.Phones!.FewerThanFive);
        Assert.Equal(0, notSignedIn.Phones.Value);
        Assert.Equal(9, Assert.Single(summary.Groups, group => group.Group == UsageGroup.SignedIn).Phones!.Value);
        LabelTally bible = Assert.Single(Assert.Single(summary.Top).Entries);
        Assert.Equal("Bible", bible.Label);
        Assert.True(bible.Count!.FewerThanFive);
        Assert.Equal(0, bible.Count.Value);
    }

    [Fact]
    public async Task TheWeeklyTrendFollowsMondayToSundayInBrisbane()
    {
        await PhonesOpenAsync(6, UsageGroup.SignedIn);
        _host.Clock.Advance(TimeSpan.FromDays(7));
        await PhonesOpenAsync(8, UsageGroup.SignedIn);

        InsightsSummary summary = await SummaryAsync();

        Assert.Equal(InsightsService.TrendWeeks, summary.Weeks.Count);
        Assert.Equal(ChurchTime.WeekStart(_host.Today).DayNumber, summary.Weeks[^1].WeekStart);
        Assert.Equal([6, 8], summary.Weeks.TakeLast(2).Select(week => week.Phones!.Value));
        Assert.Equal(6, summary.LastWeek!.Value);
        Assert.Equal(8, summary.ThisWeek!.Value);
    }

    [Fact]
    public async Task DaysOlderThanTwentyFiveMonthsFoldIntoMonthlyTotals()
    {
        await PhonesOpenAsync(6, UsageGroup.SignedIn);
        await PhonesOpenAsync(2, UsageGroup.NotSignedIn);
        DateOnly opened = _host.Today;

        _host.Clock.Advance(TimeSpan.FromDays(800));
        Assert.True(await _host.WithAsync<AnalyticsRetention, bool>(retention => retention.PruneAsync(AnalyticsHost.Cancellation)));

        (List<DailyCountRecord> daily, List<MonthlyCountRecord> monthly) = await _host.WithAsync<AnalyticsDbContext, (List<DailyCountRecord>, List<MonthlyCountRecord>)>(async db =>
            (await db.DailyCounts.ToListAsync(AnalyticsHost.Cancellation), await db.MonthlyCounts.ToListAsync(AnalyticsHost.Cancellation)));
        Assert.Empty(daily);
        Assert.Equal(8, monthly.Where(count => count.Measure == AnalyticsMeasures.ActiveWeek.Key).Sum(count => count.Count));
        Assert.All(monthly, count => Assert.Equal(new DateOnly(opened.Year, opened.Month, 1), count.Month));
    }

    [Fact]
    public async Task APhoneSaysHowItIsSetUpOnceAWeekAndOnlyInDeclaredWords()
    {
        StateMemory phone = new();
        DateOnly monday = new(2026, 11, 2);
        Measure notifications = AnalyticsHost.Measures.Notifications;
        Assert.True(phone.TakeIfDue(notifications, monday));
        Assert.False(phone.TakeIfDue(notifications, monday));
        Assert.True(phone.TakeIfDue(notifications, monday.AddDays(7)));

        for (int on = 0; on < 6; on++)
            await StateAsync("on");
        await StateAsync("off");
        await StateAsync("sam@example.org");

        TopList state = Assert.Single((await SummaryAsync()).States);
        Assert.Equal("Notifications", state.Label);
        Assert.Equal(["On", "Turned off"], state.Entries.Select(entry => entry.Label));
        Assert.Equal(6, state.Entries[0].Count!.Value);
        Assert.True(state.Entries[1].Count!.FewerThanFive);
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private Task StateAsync(string state) =>
        AcceptAsync(Report(new UsageCount { Day = _host.Today.DayNumber, Measure = AnalyticsHost.Measures.Notifications.Key, Subject = state, Count = 1 }), Guid.NewGuid().ToString());

    private static IReadOnlyList<Measure> Open(OpeningMemory phone, DateOnly day) => phone.Open(day, ChurchTime.WeekStart(day));

    private async Task PhonesOpenAsync(int phones, UsageGroup group)
    {
        for (int phone = 0; phone < phones; phone++)
            await AcceptAsync(Report(new UsageCount { Day = _host.Today.DayNumber, Measure = AnalyticsMeasures.ActiveWeek.Key, Group = group, Count = 1 }), Guid.NewGuid().ToString());
    }

    private Task TapAsync(string tile, int times) =>
        AcceptAsync(Report(new UsageCount { Day = _host.Today.DayNumber, Measure = TileTapped, Subject = tile, Group = UsageGroup.SignedIn, Count = times }), Guid.NewGuid().ToString());

    private static UsageReport Report(params UsageCount[] counts) => new()
    {
        Context = new ReportContext { Origin = ReportOrigin.App, Platform = DevicePlatform.Ios, AppVersion = "1.0", AppBuild = "41", OsMajor = 26 },
        Counts = [.. counts],
    };

    private Task AcceptAsync(UsageReport report, string requestId) =>
        _host.WithAsync<UsageInbox>(inbox => inbox.AcceptAsync(report, requestId, AnalyticsHost.Cancellation));

    private Task<InsightsSummary> SummaryAsync() =>
        _host.WithAsync<InsightsService, InsightsSummary>(insights => insights.GetSummaryAsync(new InsightsQuery()).AsTask());

    private sealed class TileNames : ISubjectNames
    {
        public string SubjectKind => SubjectKinds.HubTile;

        public ValueTask<IReadOnlyDictionary<string, string>> NamesAsync(IReadOnlyCollection<string> subjects, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string> { [BibleTile] = "Bible" });
    }
}
