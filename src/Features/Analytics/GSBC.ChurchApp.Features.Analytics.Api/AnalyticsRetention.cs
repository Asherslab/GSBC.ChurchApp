using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class AnalyticsRetention(AnalyticsDbContext db, TimeProvider time)
{
    public static readonly TimeSpan ErrorsKeptFor = TimeSpan.FromDays(30);
    public static readonly TimeSpan RequestIdsKeptFor = TimeSpan.FromDays(7);
    public const int DailyCountsKeptForMonths = 25;
    private const long AdvisoryLockKey = 0x6773_6263_616e_6c79;

    public async Task<bool> PruneAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        bool locked = await db.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({AdvisoryLockKey}) AS \"Value\"")
            .SingleAsync(cancellationToken);
        if (!locked)
            return false;

        DateTime now = time.GetUtcNow().UtcDateTime;
        DateTime errorsBefore = now - ErrorsKeptFor;
        DateOnly daysBefore = ChurchTime.DayOf(errorsBefore);

        await db.ErrorReports.Where(report => report.ReceivedAt < errorsBefore || report.Day < daysBefore).ExecuteDeleteAsync(cancellationToken);
        await db.ErrorKinds.Where(kind => kind.LastSeen < errorsBefore).ExecuteDeleteAsync(cancellationToken);
        await db.FailedCalls.Where(call => call.Day < daysBefore).ExecuteDeleteAsync(cancellationToken);
        await db.SeenReports.Where(seen => seen.SeenAt < now - RequestIdsKeptFor).ExecuteDeleteAsync(cancellationToken);

        DateOnly today = ChurchTime.Today(time);
        DateOnly foldBefore = new DateOnly(today.Year, today.Month, 1).AddMonths(-DailyCountsKeptForMonths);
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO analytics.monthly_counts (month, measure, subject, "group", platform, app_version, count)
            SELECT date_trunc('month', day)::date, measure, subject, "group", platform, app_version, SUM(count)
            FROM analytics.daily_counts WHERE day < {foldBefore}
            GROUP BY 1, measure, subject, "group", platform, app_version
            ON CONFLICT (month, measure, subject, "group", platform, app_version) DO UPDATE SET count = analytics.monthly_counts.count + EXCLUDED.count
            """, cancellationToken);
        await db.DailyCounts.Where(count => count.Day < foldBefore).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
