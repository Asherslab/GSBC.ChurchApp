using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class DiagnosticsInbox(AnalyticsDbContext db, NewErrorKinds newKinds, TimeProvider time)
{
    private const int FingerprintFrames = 5;
    private const int MaximumCountPerLine = 1_000;
    private const int MaximumTypeLength = 200;
    private const int MaximumRouteLength = 200;
    private const int MaximumMethodLength = 200;
    private const int MaximumVersionLength = 32;
    private const int OldestDayAccepted = 30;

    public async Task AcceptAsync(DiagnosticsReport report, string? requestId, CancellationToken cancellationToken)
    {
        ReportContext context = report.Context ?? new ReportContext();
        if (context.Origin is not (ReportOrigin.App or ReportOrigin.Admin))
            return;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (requestId is not null && !await FirstTimeSeenAsync(requestId, cancellationToken))
            return;

        DateOnly today = ChurchTime.Today(time);
        List<Guid> created = [];
        foreach (ErrorOccurrence error in report.Errors.Take(DiagnosticsReport.MaximumErrors))
        {
            if (DayOf(error.Day, today) is not { } day || string.IsNullOrWhiteSpace(error.ExceptionType))
                continue;

            ErrorSighting sighting = new(context.Origin, error.NativeCrash, Clip(error.ExceptionType, MaximumTypeLength),
                CleanFrames(error.Frames), Clip(error.Route, MaximumRouteLength), day, CountOf(error.Count),
                context.Platform, Clip(error.AppVersion ?? context.AppVersion, MaximumVersionLength),
                Clip(error.AppBuild ?? context.AppBuild, MaximumVersionLength), context.OsMajor, context.DevelopmentBuild);
            if (await RecordAsync(sighting, cancellationToken) is { } kind)
                created.Add(kind);
        }

        foreach (FailedCallCount call in report.FailedCalls.Take(DiagnosticsReport.MaximumFailedCalls))
        {
            if (DayOf(call.Day, today) is not { } day || !LooksLikeAMethod(call.Method) || !Enum.IsDefined((StatusCode)call.Status))
                continue;
            await CountFailedCallAsync(day, context.Origin, call.Method, call.Status, Clip(context.AppVersion, MaximumVersionLength),
                CountOf(call.Count), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        await newKinds.AnnounceAsync(created, cancellationToken);
    }

    public async Task RecordServerFailureAsync(string method, StatusCode status, Exception? unexpected, CancellationToken cancellationToken)
    {
        DateOnly today = ChurchTime.Today(time);
        await CountFailedCallAsync(today, ReportOrigin.Server, Clip(method, MaximumMethodLength), (int)status, "", 1, cancellationToken);
        if (unexpected is null)
            return;

        ErrorSighting sighting = new(ReportOrigin.Server, false, Clip(StackSummary.TypeOf(unexpected), MaximumTypeLength),
            StackSummary.FramesOf(unexpected), Clip(method, MaximumRouteLength), today, 1, DevicePlatform.Unknown, "", "", 0, false);
        if (await RecordAsync(sighting, cancellationToken) is { } kind)
            await newKinds.AnnounceAsync([kind], cancellationToken);
    }

    private async Task<Guid?> RecordAsync(ErrorSighting sighting, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        string where = sighting.Frames.FirstOrDefault() ?? sighting.Route;
        List<string> frames = sighting.Frames;

        List<KindUpsert> upserted = await db.Database.SqlQuery<KindUpsert>($"""
            INSERT INTO analytics.error_kinds (id, fingerprint, origin, native_crash, exception_type, frames, "where", first_seen, last_seen, count)
            VALUES ({Guid.NewGuid()}, {Fingerprint(sighting)}, {(int)sighting.Origin}, {sighting.NativeCrash}, {sighting.ExceptionType},
                    {frames}, {where}, {now}, {now}, {sighting.Count})
            ON CONFLICT (fingerprint) DO UPDATE SET last_seen = EXCLUDED.last_seen, count = analytics.error_kinds.count + EXCLUDED.count
            RETURNING id, (xmax = 0) AS created
            """).ToListAsync(cancellationToken);
        KindUpsert kind = upserted.Single();

        db.ErrorReports.Add(new ErrorReportRecord
        {
            KindId = kind.Id,
            Day = sighting.Day,
            Platform = sighting.Platform,
            AppVersion = sighting.AppVersion,
            AppBuild = sighting.AppBuild,
            OsMajor = sighting.OsMajor,
            DevelopmentBuild = sighting.DevelopmentBuild,
            Route = sighting.Route,
            Count = sighting.Count,
            ReceivedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        return kind.Created ? kind.Id : null;
    }

    private Task CountFailedCallAsync(DateOnly day, ReportOrigin origin, string method, int status, string appVersion, int count, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlAsync($"""
            INSERT INTO analytics.failed_calls (day, origin, method, status, app_version, count)
            VALUES ({day}, {(int)origin}, {method}, {status}, {appVersion}, {(long)count})
            ON CONFLICT (day, origin, method, status, app_version) DO UPDATE SET count = analytics.failed_calls.count + EXCLUDED.count
            """, cancellationToken);

    private async Task<bool> FirstTimeSeenAsync(string requestId, CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO analytics.reports_seen (request_id, seen_at) VALUES ({Clip(requestId, 64)}, {time.GetUtcNow().UtcDateTime})
            ON CONFLICT (request_id) DO NOTHING
            """, cancellationToken) == 1;

    internal static string Fingerprint(ErrorSighting sighting)
    {
        string identity = string.Join('\n', [$"{sighting.Origin}", $"{sighting.NativeCrash}", sighting.ExceptionType,
            .. sighting.Frames.Take(FingerprintFrames), sighting.Frames.Count == 0 ? sighting.Route : ""]);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    private static DateOnly? DayOf(int dayNumber, DateOnly today)
    {
        if (dayNumber < today.DayNumber - OldestDayAccepted || dayNumber > today.DayNumber + 1)
            return null;
        return DateOnly.FromDayNumber(dayNumber);
    }

    private static List<string> CleanFrames(IEnumerable<string> frames) =>
        frames.Where(frame => !string.IsNullOrWhiteSpace(frame))
            .Take(StackSummary.MaximumFrames)
            .Select(frame => Clip(frame, StackSummary.MaximumFrameLength))
            .ToList();

    private static bool LooksLikeAMethod(string method) =>
        method.Length <= MaximumMethodLength && method.StartsWith('/') && method.Count(character => character == '/') == 2;

    private static int CountOf(int count) => Math.Clamp(count, 1, MaximumCountPerLine);

    private static string Clip(string? text, int length)
    {
        string trimmed = (text ?? "").Trim();
        return trimmed.Length > length ? trimmed[..length] : trimmed;
    }

    private sealed record KindUpsert(Guid Id, bool Created);
}

internal sealed record ErrorSighting(
    ReportOrigin Origin, bool NativeCrash, string ExceptionType, List<string> Frames, string Route, DateOnly Day, int Count,
    DevicePlatform Platform, string AppVersion, string AppBuild, int OsMajor, bool DevelopmentBuild);
