using System.Text.RegularExpressions;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.Analytics;
using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed record UsageLine(DateOnly Day, Measure Measure, string Subject, UsageGroup Group, int Count);

internal sealed record UsageLines(DevicePlatform Platform, string AppVersion, IReadOnlyList<UsageLine> Lines);

internal static partial class UsageReading
{
    private const int MaximumCountPerLine = 500;
    private const int MaximumSubjectLength = 64;
    private const int MaximumVersionLength = 32;
    private const int OldestDayAccepted = 30;

    public static UsageLines? Read(UsageReport report, MeasureCatalog measures, DateOnly today)
    {
        ReportContext context = report.Context ?? new ReportContext();
        if (context.Origin != ReportOrigin.App)
            return null;

        string version = context.AppVersion.Length > MaximumVersionLength ? context.AppVersion[..MaximumVersionLength] : context.AppVersion;
        List<UsageLine> lines = report.Counts
            .Take(UsageReport.MaximumCounts)
            .Select(line => Clean(line, measures, today))
            .OfType<UsageLine>()
            .GroupBy(line => (line.Day, line.Measure.Key, line.Subject, line.Group))
            .Select(group => group.First() with { Count = Cap(group.First().Measure, group.Sum(line => line.Count)) })
            .ToList();
        return new UsageLines(context.Platform, version, lines);
    }

    private static UsageLine? Clean(UsageCount line, MeasureCatalog measures, DateOnly today)
    {
        if (!measures.ByKey.TryGetValue(line.Measure, out Measure? measure)
            || line.Day < today.DayNumber - OldestDayAccepted || line.Day > today.DayNumber + 1 || line.Count <= 0)
            return null;

        string? subject = SubjectOf(measure, line.Subject.Trim());
        if (subject is null)
            return null;

        UsageGroup group = measure.SplitByGroup && Enum.IsDefined(line.Group) ? line.Group : UsageGroup.Everyone;
        return new UsageLine(DateOnly.FromDayNumber(line.Day), measure, subject, group, line.Count);
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
}
