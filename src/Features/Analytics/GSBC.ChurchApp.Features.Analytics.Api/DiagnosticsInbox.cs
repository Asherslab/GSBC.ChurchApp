using Grpc.Core;
using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class DiagnosticsInbox(AnalyticsDbContext db, NewErrorKinds newKinds, ReportsSeen seen, TimeProvider time)
{
    public async Task AcceptAsync(DiagnosticsReport report, string? requestId, CancellationToken cancellationToken)
    {
        if (DiagnosticsReading.Read(report, ChurchTime.Today(time)) is not { } lines)
            return;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (requestId is not null && !await seen.FirstTimeAsync(requestId, cancellationToken))
            return;

        List<Guid> created = [];
        foreach (ErrorSighting sighting in lines.Sightings)
        {
            if (await RecordAsync(sighting, cancellationToken) is { } kind)
                created.Add(kind);
        }

        foreach (FailedCallLine call in lines.FailedCalls)
            await CountFailedCallAsync(call, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        await newKinds.AnnounceAsync(created, cancellationToken);
    }

    public async Task RecordServerFailureAsync(string method, StatusCode status, Exception? unexpected, CancellationToken cancellationToken)
    {
        (FailedCallLine call, ErrorSighting? sighting) = DiagnosticsReading.ServerFailure(method, status, unexpected, ChurchTime.Today(time));
        await CountFailedCallAsync(call, cancellationToken);
        if (sighting is not null && await RecordAsync(sighting, cancellationToken) is { } kind)
            await newKinds.AnnounceAsync([kind], cancellationToken);
    }

    private async Task<Guid?> RecordAsync(ErrorSighting sighting, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        string where = sighting.Frames.FirstOrDefault() ?? sighting.Route;
        List<string> frames = sighting.Frames;

        List<KindUpsert> upserted = await db.Database.SqlQuery<KindUpsert>($"""
            INSERT INTO analytics.error_kinds (id, fingerprint, origin, native_crash, exception_type, frames, "where", first_seen, last_seen, count)
            VALUES ({Guid.NewGuid()}, {DiagnosticsReading.Fingerprint(sighting)}, {(int)sighting.Origin}, {sighting.NativeCrash}, {sighting.ExceptionType},
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

    private Task CountFailedCallAsync(FailedCallLine call, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlAsync($"""
            INSERT INTO analytics.failed_calls (day, origin, method, status, app_version, count)
            VALUES ({call.Day}, {(int)call.Origin}, {call.Method}, {call.Status}, {call.AppVersion}, {(long)call.Count})
            ON CONFLICT (day, origin, method, status, app_version) DO UPDATE SET count = analytics.failed_calls.count + EXCLUDED.count
            """, cancellationToken);

    private sealed record KindUpsert(Guid Id, bool Created);
}
