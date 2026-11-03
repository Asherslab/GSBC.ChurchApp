using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class AnalyticsRetention(AnalyticsDbContext db, TimeProvider time)
{
    public static readonly TimeSpan ErrorsKeptFor = TimeSpan.FromDays(30);
    public static readonly TimeSpan RequestIdsKeptFor = TimeSpan.FromDays(7);
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

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
