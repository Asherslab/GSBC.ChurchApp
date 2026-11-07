using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

public sealed class UsageReportTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);

    [Fact]
    public void APhoneCountsItselfOnceADayWeekAndMonthAndSaysWhenItCameBack()
    {
        OpeningMemory phone = new();

        Assert.Equal([AnalyticsMeasures.Installed, AnalyticsMeasures.ActiveDay, AnalyticsMeasures.ActiveWeek, AnalyticsMeasures.ActiveMonth], Open(phone, Monday));
        Assert.Empty(Open(phone, Monday));
        Assert.Equal([AnalyticsMeasures.ActiveDay], Open(phone, Monday.AddDays(2)));
        Assert.Equal([AnalyticsMeasures.ActiveDay, AnalyticsMeasures.ActiveWeek, AnalyticsMeasures.Returning], Open(phone, Monday.AddDays(7)));
        Assert.Equal([AnalyticsMeasures.ActiveDay, AnalyticsMeasures.ActiveWeek, AnalyticsMeasures.ActiveMonth], Open(phone, Monday.AddDays(35)));
    }

    [Fact]
    public void OnlyDeclaredMeasuresAreKeptAndAPhoneCanOnlyCountItselfOnce()
    {
        int today = Monday.DayNumber;
        UsageReport report = Report(
            new UsageCount { Day = today, Measure = AnalyticsMeasures.ActiveWeek.Key, Group = UsageGroup.SignedIn, Count = 40 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.ActiveWeek.Key, Group = UsageGroup.SignedIn, Count = 1 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.Installed.Key, Group = UsageGroup.OnATeam, Count = 1 },
            new UsageCount { Day = today, Measure = "prayer.request.viewed", Subject = "42", Count = 1 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.Installed.Key, Subject = "sam@example.org", Count = 1 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.ActiveDay.Key, Count = 1 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.ActiveDay.Key, Count = 1 },
            new UsageCount { Day = today, Measure = AnalyticsMeasures.ActiveDay.Key, Count = 1 },
            new UsageCount { Day = today, Measure = TestMeasures.TileTapped.Key, Subject = "Sam's prayer for her mum", Count = 3 });

        IReadOnlyList<UsageLine> kept = UsageReading.Read(report, TestMeasures.Catalog, Monday)!.Lines;

        Assert.Equal(3, kept.Count);
        Assert.All(kept, line => Assert.Equal(1, line.Count));
        Assert.All(kept, line => Assert.Equal("", line.Subject));
        Assert.Equal(UsageGroup.Everyone, Assert.Single(kept, line => line.Measure == AnalyticsMeasures.Installed).Group);
    }

    [Fact]
    public void APhoneSaysHowItIsSetUpOnceAWeekAndOnlyInDeclaredWords()
    {
        StateMemory phone = new();
        Measure notifications = TestMeasures.Notifications;
        Assert.True(phone.TakeIfDue(notifications, Monday));
        Assert.False(phone.TakeIfDue(notifications, Monday));
        Assert.True(phone.TakeIfDue(notifications, Monday.AddDays(7)));

        UsageReport report = Report(
            new UsageCount { Day = Monday.DayNumber, Measure = notifications.Key, Subject = "on", Count = 1 },
            new UsageCount { Day = Monday.DayNumber, Measure = notifications.Key, Subject = "sam@example.org", Count = 1 });

        Assert.Equal(["on"], UsageReading.Read(report, TestMeasures.Catalog, Monday)!.Lines.Select(line => line.Subject));
    }

    [Fact]
    public void OnlyTheAppReportsUsage() =>
        Assert.Null(UsageReading.Read(new UsageReport { Context = new ReportContext { Origin = ReportOrigin.Admin } }, TestMeasures.Catalog, Monday));

    private static IReadOnlyList<Measure> Open(OpeningMemory phone, DateOnly day) => phone.Open(day, ChurchTime.WeekStart(day));

    private static UsageReport Report(params UsageCount[] counts) => new()
    {
        Context = new ReportContext { Origin = ReportOrigin.App, Platform = DevicePlatform.Ios, AppVersion = "1.0", AppBuild = "41", OsMajor = 26 },
        Counts = [.. counts],
    };
}
