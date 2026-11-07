using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

[Integration("ON CONFLICT adds every phone's counts into one row per day, measure, subject, group, platform and version")]
public sealed class UsageInboxTests : IAsyncLifetime
{
    private readonly AnalyticsHost _host = new();

    public async ValueTask InitializeAsync() => await _host.StartAsync();

    [Fact]
    public async Task EachPhonesCountAddsToTheSameRow()
    {
        for (int phone = 0; phone < 9; phone++)
            await _host.WithAsync<UsageInbox>(inbox => inbox.AcceptAsync(Opened(), $"phone-{phone}", AnalyticsHost.Cancellation));

        List<DailyCountRecord> stored = await _host.WithAsync<AnalyticsDbContext, List<DailyCountRecord>>(db => db.DailyCounts.ToListAsync(AnalyticsHost.Cancellation));
        Assert.Equal(9, Assert.Single(stored).Count);
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private UsageReport Opened() => new()
    {
        Context = new ReportContext { Origin = ReportOrigin.App, Platform = DevicePlatform.Ios, AppVersion = "1.0", AppBuild = "41", OsMajor = 26 },
        Counts = [new UsageCount { Day = _host.Today.DayNumber, Measure = AnalyticsMeasures.ActiveWeek.Key, Group = UsageGroup.SignedIn, Count = 1 }],
    };
}
