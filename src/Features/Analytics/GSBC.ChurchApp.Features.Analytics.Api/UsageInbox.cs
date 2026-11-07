using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.Analytics;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class UsageInbox(AnalyticsDbContext db, MeasureCatalog measures, ReportsSeen seen, TimeProvider time)
{
    public async Task AcceptAsync(UsageReport report, string? requestId, CancellationToken cancellationToken)
    {
        if (UsageReading.Read(report, measures, ChurchTime.Today(time)) is not { } usage)
            return;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (requestId is not null && !await seen.FirstTimeAsync(requestId, cancellationToken))
            return;

        foreach (UsageLine line in usage.Lines)
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO analytics.daily_counts (day, measure, subject, "group", platform, app_version, count)
                VALUES ({line.Day}, {line.Measure.Key}, {line.Subject}, {(int)line.Group}, {(int)usage.Platform}, {usage.AppVersion}, {(long)line.Count})
                ON CONFLICT (day, measure, subject, "group", platform, app_version) DO UPDATE SET count = analytics.daily_counts.count + EXCLUDED.count
                """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
