using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.Analytics;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Analytics;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class InsightsService(AnalyticsDbContext db, MeasureCatalog measures, IEnumerable<ISubjectNames> subjectNames, TimeProvider time)
    : IInsightsService
{
    private const int WeeksInAYear = 52;

    public async ValueTask<InsightsSummary> GetSummaryAsync(InsightsQuery request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        DateOnly today = ChurchTime.Today(time);
        DateOnly weekStart = ChurchTime.WeekStart(today);
        DateOnly monthStart = new(today.Year, today.Month, 1);
        DateOnly trendStart = weekStart.AddDays(-InsightsTallies.DaysInAWeek * (InsightsTallies.TrendWeeks - 1));
        DateOnly lastYearStart = trendStart.AddDays(-InsightsTallies.DaysInAWeek * WeeksInAYear);

        List<DailyCountRecord> weekly = await db.DailyCounts
            .Where(count => count.Measure == AnalyticsMeasures.ActiveWeek.Key && count.Day >= lastYearStart)
            .ToListAsync(cancellationToken);
        List<DailyCountRecord> thisWeek = weekly.Where(count => count.Day >= weekStart).ToList();

        DateOnly tomorrow = today.AddDays(1);
        long thisMonth = await SumAsync(AnalyticsMeasures.ActiveMonth, monthStart, tomorrow, cancellationToken);
        long newThisMonth = await SumAsync(AnalyticsMeasures.Installed, monthStart, tomorrow, cancellationToken);
        long newLastMonth = await SumAsync(AnalyticsMeasures.Installed, monthStart.AddMonths(-1), monthStart, cancellationToken);
        long returning = await SumAsync(AnalyticsMeasures.Returning, weekStart, tomorrow, cancellationToken);
        DateTime? lastReport = await db.SeenReports.MaxAsync(seen => (DateTime?)seen.SeenAt, cancellationToken);
        string? latest = LatestOf(thisWeek);

        return new InsightsSummary
        {
            Today = today.DayNumber,
            WeekStart = weekStart.DayNumber,
            LastReportAt = lastReport is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Utc) : null,
            ThisWeek = Tally.Of(thisWeek.Sum(count => count.Count)),
            LastWeek = Tally.Of(weekly.Where(count => count.Day >= weekStart.AddDays(-InsightsTallies.DaysInAWeek) && count.Day < weekStart).Sum(count => count.Count)),
            ThisMonth = Tally.Of(thisMonth),
            NewThisMonth = Tally.Of(newThisMonth),
            NewLastMonth = Tally.Of(newLastMonth),
            ReturningThisWeek = Tally.Of(returning),
            Groups = InsightsTallies.GroupsOf(thisWeek),
            Weeks = InsightsTallies.Weeks(weekly, trendStart),
            WeeksLastYear = weekly.Any(count => count.Day < trendStart) ? InsightsTallies.Weeks(weekly, lastYearStart) : [],
            Top = await TopAsync(weekStart, cancellationToken),
            States = await StatesAsync(weekStart, cancellationToken),
            Platforms = thisWeek.GroupBy(count => count.Platform)
                .Select(group => new LabelTally { Label = PlatformName(group.Key), Count = Tally.Of(group.Sum(count => count.Count)) })
                .OrderByDescending(entry => entry.Count!.Value)
                .ToList(),
            Versions = thisWeek.GroupBy(count => count.AppVersion)
                .OrderByDescending(group => ParsedVersion(group.Key))
                .Select(group => new LabelTally { Label = group.Key, Count = Tally.Of(group.Sum(count => count.Count)) })
                .ToList(),
            LatestVersion = latest ?? "",
            OnLatestVersion = Tally.Of(thisWeek.Where(count => count.AppVersion == latest).Sum(count => count.Count)),
        };
    }

    private Task<long> SumAsync(Measure measure, DateOnly from, DateOnly until, CancellationToken cancellationToken) =>
        db.DailyCounts
            .Where(count => count.Measure == measure.Key && count.Day >= from && count.Day < until)
            .SumAsync(count => count.Count, cancellationToken);

    private async Task<List<TopList>> TopAsync(DateOnly weekStart, CancellationToken cancellationToken)
    {
        List<Measure> ranked = measures.ByKey.Values.Where(measure => measure.SubjectKind is not null).ToList();
        List<SubjectTotal> totals = await TotalsAsync(ranked, weekStart, cancellationToken);

        List<TopList> lists = [];
        foreach (Measure measure in ranked)
        {
            List<SubjectTotal> top = InsightsTallies.TopOf(measure, totals);
            if (top.Count == 0)
                continue;

            IReadOnlyDictionary<string, string> names = subjectNames.FirstOrDefault(candidate => candidate.SubjectKind == measure.SubjectKind) is { } naming
                ? await naming.NamesAsync(top.Select(total => total.Subject).ToList(), cancellationToken)
                : new Dictionary<string, string>();
            lists.Add(InsightsTallies.Ranked(measure, top, names));
        }
        return lists;
    }

    private async Task<List<TopList>> StatesAsync(DateOnly weekStart, CancellationToken cancellationToken)
    {
        List<Measure> stated = measures.ByKey.Values.Where(measure => measure.States is not null).ToList();
        List<SubjectTotal> totals = await TotalsAsync(stated, weekStart, cancellationToken);
        return stated
            .Where(measure => totals.Any(total => total.Measure == measure.Key))
            .Select(measure => InsightsTallies.Stated(measure, totals))
            .ToList();
    }

    private Task<List<SubjectTotal>> TotalsAsync(List<Measure> of, DateOnly weekStart, CancellationToken cancellationToken)
    {
        List<string> keys = of.Select(measure => measure.Key).ToList();
        return db.DailyCounts
            .Where(count => keys.Contains(count.Measure) && count.Day >= weekStart)
            .GroupBy(count => new { count.Measure, count.Subject })
            .Select(group => new SubjectTotal(group.Key.Measure, group.Key.Subject, group.Sum(count => count.Count)))
            .ToListAsync(cancellationToken);
    }

    private static string? LatestOf(List<DailyCountRecord> counts) =>
        counts.Select(count => count.AppVersion).Where(version => version.Length > 0).OrderByDescending(ParsedVersion).FirstOrDefault();

    private static Version ParsedVersion(string version) => Version.TryParse(version, out Version? parsed) ? parsed : new Version(0, 0);

    private static string PlatformName(DevicePlatform platform) => platform switch
    {
        DevicePlatform.Ios => "iPhone and iPad",
        DevicePlatform.Android => "Android",
        _ => "Other",
    };
}
