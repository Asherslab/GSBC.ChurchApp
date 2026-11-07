using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

[Integration("Pruning deletes errors after 30 days and request ids after 7, and folds days older than 25 months into monthly totals")]
public sealed class AnalyticsRetentionTests : IAsyncLifetime
{
    private readonly AnalyticsHost _host = new();

    public async ValueTask InitializeAsync() => await _host.StartAsync();

    [Fact]
    public async Task ErrorsGoAfterThirtyDaysAndAResentReportIdIsForgottenAfterSeven()
    {
        await AcceptAsync(CrashReport(), requestId: "phone-a-1");

        _host.Clock.Advance(TimeSpan.FromDays(8));
        await PruneAsync();
        await AcceptAsync(CrashReport(), requestId: "phone-a-1");
        Assert.Equal(2, Assert.Single(await _host.WithAsync<AnalyticsDbContext, List<ErrorKindRecord>>(db => db.ErrorKinds.ToListAsync(Cancellation))).Count);

        _host.Clock.Advance(TimeSpan.FromDays(31));
        await PruneAsync();
        await _host.WithAsync<AnalyticsDbContext>(async db =>
        {
            Assert.Empty(await db.ErrorKinds.ToListAsync(Cancellation));
            Assert.Empty(await db.ErrorReports.ToListAsync(Cancellation));
        });
    }

    [Fact]
    public async Task DaysOlderThanTwentyFiveMonthsFoldIntoMonthlyTotals()
    {
        await OpenAsync(6, UsageGroup.SignedIn);
        await OpenAsync(2, UsageGroup.NotSignedIn);
        DateOnly opened = _host.Today;

        _host.Clock.Advance(TimeSpan.FromDays(800));
        await PruneAsync();

        (List<DailyCountRecord> daily, List<MonthlyCountRecord> monthly) = await _host.WithAsync<AnalyticsDbContext, (List<DailyCountRecord>, List<MonthlyCountRecord>)>(async db =>
            (await db.DailyCounts.ToListAsync(Cancellation), await db.MonthlyCounts.ToListAsync(Cancellation)));
        Assert.Empty(daily);
        Assert.Equal(8, monthly.Where(count => count.Measure == AnalyticsMeasures.ActiveWeek.Key).Sum(count => count.Count));
        Assert.All(monthly, count => Assert.Equal(new DateOnly(opened.Year, opened.Month, 1), count.Month));
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private DiagnosticsReport CrashReport() => new()
    {
        Context = Phone,
        Errors = [new ErrorOccurrence { Day = _host.Today.DayNumber, ExceptionType = "System.NullReferenceException", Frames = ["GSBC.ChurchApp.Features.Sermons.Client.Player.Start"], Count = 1 }],
    };

    private async Task OpenAsync(int phones, UsageGroup group)
    {
        for (int phone = 0; phone < phones; phone++)
            await _host.WithAsync<UsageInbox>(inbox => inbox.AcceptAsync(new UsageReport
            {
                Context = Phone,
                Counts = [new UsageCount { Day = _host.Today.DayNumber, Measure = AnalyticsMeasures.ActiveWeek.Key, Group = group, Count = 1 }],
            }, Guid.NewGuid().ToString(), Cancellation));
    }

    private Task AcceptAsync(DiagnosticsReport report, string requestId) =>
        _host.WithAsync<DiagnosticsInbox>(inbox => inbox.AcceptAsync(report, requestId, Cancellation));

    private async Task PruneAsync() =>
        Assert.True(await _host.WithAsync<AnalyticsRetention, bool>(retention => retention.PruneAsync(Cancellation)));

    private static ReportContext Phone => new() { Origin = ReportOrigin.App, Platform = DevicePlatform.Ios, AppVersion = "1.0", AppBuild = "41", OsMajor = 26 };

    private static CancellationToken Cancellation => AnalyticsHost.Cancellation;
}
