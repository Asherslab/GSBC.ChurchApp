using Grpc.Core;
using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class HealthService(AnalyticsDbContext db, AnalyticsEnvironment environment, TimeProvider time) : IHealthService
{
    private const int LongestWindowDays = 30;
    private const int MostKinds = 200;

    public async ValueTask<HealthOverview> GetOverviewAsync(HealthQuery request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        DateOnly since = Since(request.Days);

        var sightings = await db.ErrorReports
            .Where(report => report.Day >= since)
            .GroupBy(report => report.KindId)
            .Select(group => new
            {
                KindId = group.Key,
                Count = group.Sum(report => (long)report.Count),
                Versions = group.Select(report => report.AppVersion).Distinct().ToList(),
                OnlyDevelopment = group.All(report => report.DevelopmentBuild),
            })
            .ToListAsync(cancellationToken);

        List<Guid> ids = sightings.Select(sighting => sighting.KindId).ToList();
        Dictionary<Guid, ErrorKindRecord> kinds = await db.ErrorKinds.Where(kind => ids.Contains(kind.Id)).ToDictionaryAsync(kind => kind.Id, cancellationToken);

        List<FailedCallSummary> calls = await db.FailedCalls
            .Where(call => call.Day >= since)
            .GroupBy(call => new { call.Origin, call.Method, call.Status })
            .Select(group => new FailedCallSummary
            {
                Origin = group.Key.Origin,
                Method = group.Key.Method,
                Status = group.Key.Status,
                Count = group.Sum(call => call.Count),
                LastDay = group.Max(call => call.Day).DayNumber,
            })
            .OrderByDescending(call => call.Count)
            .Take(MostKinds)
            .ToListAsync(cancellationToken);

        return new HealthOverview
        {
            Environment = environment.Name,
            Kinds = sightings
                .Where(sighting => kinds.ContainsKey(sighting.KindId))
                .Select(sighting => Summary(kinds[sighting.KindId], sighting.Count, sighting.Versions, sighting.OnlyDevelopment))
                .OrderByDescending(summary => summary.LastSeen)
                .Take(MostKinds)
                .ToList(),
            FailedCalls = calls,
        };
    }

    public async ValueTask<ErrorKindDetail> GetErrorKindAsync(ErrorKindRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        ErrorKindRecord kind = await db.ErrorKinds.SingleOrDefaultAsync(record => record.Id == request.Id, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That error is no longer kept: errors go after 30 days."));

        List<ErrorReportRecord> reports = await db.ErrorReports.Where(report => report.KindId == kind.Id).ToListAsync(cancellationToken);

        return new ErrorKindDetail
        {
            Kind = Summary(kind, reports.Sum(report => (long)report.Count), reports.Select(report => report.AppVersion).Distinct().ToList(),
                reports.All(report => report.DevelopmentBuild)),
            Frames = kind.Frames,
            Routes = CountBy(reports, report => report.Route),
            Platforms = CountBy(reports, report => HealthWords.Platform(report.Platform, report.OsMajor)),
            Builds = CountBy(reports, report => HealthWords.Build(report.AppVersion, report.AppBuild, report.DevelopmentBuild)),
            Days = reports.GroupBy(report => report.Day)
                .Select(group => new CountByDay { Day = group.Key.DayNumber, Count = group.Sum(report => (long)report.Count) })
                .OrderBy(day => day.Day)
                .ToList(),
        };
    }

    private DateOnly Since(int days) => ChurchTime.Today(time).AddDays(1 - Math.Clamp(days <= 0 ? LongestWindowDays : days, 1, LongestWindowDays));

    private static ErrorKindSummary Summary(ErrorKindRecord kind, long count, List<string> versions, bool onlyDevelopment) => new()
    {
        Id = kind.Id,
        Origin = kind.Origin,
        NativeCrash = kind.NativeCrash,
        ExceptionType = kind.ExceptionType,
        Where = kind.Where,
        Count = count,
        FirstSeen = DateTime.SpecifyKind(kind.FirstSeen, DateTimeKind.Utc),
        LastSeen = DateTime.SpecifyKind(kind.LastSeen, DateTimeKind.Utc),
        AppVersions = versions.Where(version => version.Length > 0).Order().ToList(),
        OnlyDevelopmentBuilds = onlyDevelopment,
    };

    private static List<CountByLabel> CountBy(IEnumerable<ErrorReportRecord> reports, Func<ErrorReportRecord, string> label) =>
        reports.GroupBy(label)
            .Select(group => new CountByLabel { Label = group.Key, Count = group.Sum(report => (long)report.Count) })
            .Where(entry => entry.Label.Length > 0)
            .OrderByDescending(entry => entry.Count)
            .ToList();
}
