using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Platform.Api.Sync;

public sealed class FeedRetention(SyncDbContext db)
{
    public static readonly TimeSpan DeletionsKeptFor = TimeSpan.FromDays(90);
    private const long AdvisoryLockKey = 0x6773_6263_7379_6e63;

    public async Task<bool> PruneAsync(DateTime now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        bool locked = await db.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({AdvisoryLockKey}) AS \"Value\"")
            .SingleAsync(cancellationToken);
        if (!locked)
            return false;

        await db.Database.ExecuteSqlAsync($"""
            DELETE FROM sync.feed_changes AS older
            USING sync.feed_changes AS newer
            WHERE newer.feed = older.feed AND newer.record_id = older.record_id AND newer.cursor > older.cursor
            """, cancellationToken);

        DateTime deletedBefore = now - DeletionsKeptFor;
        await db.Database.ExecuteSqlAsync($"""
            WITH expired AS (
                DELETE FROM sync.feed_changes
                WHERE deleted AND committed_at < {deletedBefore}
                RETURNING feed, cursor
            )
            INSERT INTO sync.feed_horizons (feed, cursor)
            SELECT feed, MAX(cursor) FROM expired GROUP BY feed
            ON CONFLICT (feed) DO UPDATE SET cursor = GREATEST(sync.feed_horizons.cursor, EXCLUDED.cursor)
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
