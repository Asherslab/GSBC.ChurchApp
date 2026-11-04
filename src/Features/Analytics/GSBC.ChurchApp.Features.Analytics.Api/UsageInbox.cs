using System.Text.RegularExpressions;
using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.Analytics;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Analytics;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed partial class UsageInbox(AnalyticsDbContext db, MeasureCatalog measures, ReportsSeen seen, TimeProvider time)
{
    public const int MaximumCountPerLine = 500;
    private const int MaximumSubjectLength = 64;
    private const int MaximumVersionLength = 32;
    private const int OldestDayAccepted = 30;

    public async Task AcceptAsync(UsageReport report, string? requestId, CancellationToken cancellationToken)
    {
        ReportContext context = report.Context ?? new ReportContext();
        if (context.Origin != ReportOrigin.App)
            return;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (requestId is not null && !await seen.FirstTimeAsync(requestId, cancellationToken))
            return;

        DateOnly today = ChurchTime.Today(time);
        string version = context.AppVersion.Length > MaximumVersionLength ? context.AppVersion[..MaximumVersionLength] : context.AppVersion;
        IEnumerable<CleanLine> lines = report.Counts
            .Take(UsageReport.MaximumCounts)
            .Select(line => Clean(line, today))
            .OfType<CleanLine>()
            .GroupBy(line => (line.Day, line.Measure.Key, line.Subject, line.Group))
            .Select(group => group.First() with { Count = Cap(group.First().Measure, group.Sum(line => line.Count)) });

        foreach (CleanLine line in lines)
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO analytics.daily_counts (day, measure, subject, "group", platform, app_version, count)
                VALUES ({line.Day}, {line.Measure.Key}, {line.Subject}, {(int)line.Group}, {(int)context.Platform}, {version}, {(long)line.Count})
                ON CONFLICT (day, measure, subject, "group", platform, app_version) DO UPDATE SET count = analytics.daily_counts.count + EXCLUDED.count
                """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private CleanLine? Clean(UsageCount line, DateOnly today)
    {
        if (!measures.ByKey.TryGetValue(line.Measure, out Measure? measure)
            || line.Day < today.DayNumber - OldestDayAccepted || line.Day > today.DayNumber + 1 || line.Count <= 0)
            return null;

        string? subject = SubjectOf(measure, line.Subject.Trim());
        if (subject is null)
            return null;

        UsageGroup group = measure.SplitByGroup && Enum.IsDefined(line.Group) ? line.Group : UsageGroup.Everyone;
        return new CleanLine(DateOnly.FromDayNumber(line.Day), measure, subject, group, line.Count);
    }

    private static string? SubjectOf(Measure measure, string subject) => measure switch
    {
        { States: { } states } => states.Any(state => state.Key == subject) ? subject : null,
        { SubjectKind: null } => "",
        _ => subject.Length is > 0 and <= MaximumSubjectLength && SafeSubject().IsMatch(subject) ? subject : null,
    };

    private static int Cap(Measure measure, int count) => measure.OncePerPhone ? 1 : Math.Min(count, MaximumCountPerLine);

    [GeneratedRegex("^[A-Za-z0-9._:-]+$")]
    private static partial Regex SafeSubject();

    private sealed record CleanLine(DateOnly Day, Measure Measure, string Subject, UsageGroup Group, int Count);
}
