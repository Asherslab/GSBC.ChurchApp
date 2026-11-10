using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Platform.Api.Sync;

public sealed class SyncService(
    SyncDbContext db, FeedRegistry feeds, IAccessEvaluator access, FeedChangeBroadcaster broadcaster, ISignedInSessions sessions, TimeProvider time) : ISyncService
{
    private const int PageSize = 500;
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AudienceMarkResolution = TimeSpan.FromDays(1);
    private static readonly string AnyRoleMembership = RoleMembership.Prefix + "any";

    public async ValueTask<PullReply> PullAsync(PullRequest request, CallContext context = default)
    {
        Dictionary<string, long> cursors = CursorsOf(request.Cursors);
        string origin = await OriginAsync(context.CancellationToken);
        HashSet<string> latest = await StartAtLatestAsync(request.Cursors, cursors, context.CancellationToken);
        return new PullReply
        {
            Feeds = await ReadAsync(cursors, cursors.Keys, UserOf(context), context.CancellationToken, latest, FromAnotherOrigin(request.Origin, origin, request.Cursors)),
            Origin = origin,
        };
    }

    public async IAsyncEnumerable<WatchEvent> WatchAsync(WatchRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        ClaimsPrincipal user = UserOf(context);
        Dictionary<string, long> cursors = CursorsOf(request.Cursors);
        using FeedChangeBroadcaster.Subscription subscription = broadcaster.Subscribe();
        string origin = await OriginAsync(cancellationToken);
        HashSet<string> latest = await StartAtLatestAsync(request.Cursors, cursors, cancellationToken);
        HashSet<string> stale = FromAnotherOrigin(request.Origin, origin, request.Cursors);

        IEnumerable<string> due = cursors.Keys;
        bool caughtUp = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            List<FeedBatch> batches = await ReadAsync(cursors, due, user, cancellationToken, caughtUp ? [] : latest, caughtUp ? [] : stale);
            if (batches.Count > 0 || !caughtUp)
                yield return new WatchEvent { Feeds = batches, Origin = origin };
            caughtUp = true;

            if (batches.Any(batch => batch.HasMore))
            {
                due = batches.Where(batch => batch.HasMore).Select(batch => batch.Feed).ToList();
                continue;
            }

            ChangedFeeds? changed = await subscription.WaitAsync(HeartbeatInterval, cancellationToken);
            await EnsureSessionCurrentAsync(user, cancellationToken);
            if (changed is null)
            {
                yield return new WatchEvent { IsHeartbeat = true, Origin = origin };
                due = [];
            }
            else
            {
                due = cursors.Keys.Where(changed.Includes).ToList();
            }
        }
    }

    private async Task<string> OriginAsync(CancellationToken cancellationToken) =>
        (await db.Origin.AsNoTracking().Select(origin => origin.Id).SingleAsync(cancellationToken)).ToString();

    private static HashSet<string> FromAnotherOrigin(string? requested, string origin, List<FeedCursor> cursors) =>
        !string.IsNullOrEmpty(requested) && requested != origin ? cursors.Where(cursor => !cursor.Latest).Select(cursor => cursor.Feed).ToHashSet() : [];

    private async Task<HashSet<string>> StartAtLatestAsync(List<FeedCursor> requested, Dictionary<string, long> cursors, CancellationToken cancellationToken)
    {
        HashSet<string> latest = requested.Where(cursor => cursor.Latest && feeds.ByName.ContainsKey(cursor.Feed)).Select(cursor => cursor.Feed).ToHashSet();
        if (latest.Count == 0)
            return latest;

        Dictionary<string, long> heads = await db.FeedChanges.AsNoTracking()
            .Where(change => latest.Contains(change.Feed))
            .GroupBy(change => change.Feed)
            .Select(group => new { Feed = group.Key, Cursor = group.Max(change => change.Cursor) })
            .ToDictionaryAsync(head => head.Feed, head => head.Cursor, cancellationToken);
        foreach (string feed in latest)
            cursors[feed] = heads.GetValueOrDefault(feed);
        return latest;
    }

    private async Task EnsureSessionCurrentAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (Caller.SessionId(user) is { } session && !await sessions.IsCurrentAsync(session, cancellationToken))
            throw new RpcException(new Status(StatusCode.Unauthenticated, "That sign-in has ended."));
    }

    private async Task<List<FeedBatch>> ReadAsync(
        Dictionary<string, long> cursors, IEnumerable<string> due, ClaimsPrincipal user, CancellationToken cancellationToken,
        IReadOnlySet<string> announce, IReadOnlySet<string> stale)
    {
        List<FeedBatch> batches = [];
        Dictionary<string, long> horizons = await db.FeedHorizons.AsNoTracking()
            .ToDictionaryAsync(horizon => horizon.Feed, horizon => horizon.Cursor, cancellationToken);

        foreach (string name in due)
        {
            if (!feeds.ByName.TryGetValue(name, out FeedDefinition? feed))
            {
                if (stale.Contains(name))
                    batches.Add(new FeedBatch { Feed = name, Reset = true });
                continue;
            }

            bool behindHorizon = cursors[name] > 0 && cursors[name] < horizons.GetValueOrDefault(name);
            bool audienceChanged = await AudienceChangedAsync(feed, user, cancellationToken) && cursors[name] > 0;
            FeedBatch batch;
            if (!await FeedVisibility.CanSeeFeedAsync(feed, user, access, cancellationToken))
            {
                batch = new FeedBatch { Feed = feed.Name, Reset = true };
            }
            else if (behindHorizon || audienceChanged || stale.Contains(name))
            {
                batch = Restart(feed);
            }
            else
            {
                batch = await ReadBatchAsync(feed, cursors[name], user, cancellationToken);
                if (batch.Cursor == cursors[name] && await IsAheadAsync(feed, cursors[name], horizons, cancellationToken))
                    batch = Restart(feed);
            }

            if (batch.Reset || batch.Cursor != cursors[name] || announce.Contains(name))
            {
                cursors[name] = batch.Cursor;
                batches.Add(batch);
            }
        }

        return batches;
    }

    private static FeedBatch Restart(FeedDefinition feed) => new() { Feed = feed.Name, Reset = true, Cursor = 0, HasMore = true };

    private async Task<bool> IsAheadAsync(FeedDefinition feed, long cursor, Dictionary<string, long> horizons, CancellationToken cancellationToken)
    {
        if (cursor <= horizons.GetValueOrDefault(feed.Name))
            return false;
        long? head = await db.FeedChanges.AsNoTracking().Where(change => change.Feed == feed.Name).MaxAsync(change => (long?)change.Cursor, cancellationToken);
        return cursor > (head ?? 0);
    }

    private async Task<FeedBatch> ReadBatchAsync(FeedDefinition feed, long after, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        IQueryable<FeedChangeRecord> changes = db.FeedChanges.AsNoTracking().Where(change => change.Feed == feed.Name && change.Cursor > after);
        if (feed.Audience is FeedAudience.OwnRecordsAudience)
        {
            string owner = Caller.UserId(user)?.ToString() ?? "";
            changes = changes.Where(change => change.ScopeId == owner);
        }

        List<FeedChangeRecord> rows = await changes
            .OrderBy(change => change.Cursor)
            .Take(PageSize + 1)
            .ToListAsync(cancellationToken);

        bool hasMore = rows.Count > PageSize;
        if (hasMore)
            rows.RemoveAt(PageSize);

        List<FeedChangeRecord> visible = await FeedVisibility.VisibleRowsAsync(feed, rows, user, access, cancellationToken);

        return new FeedBatch
        {
            Feed = feed.Name,
            Cursor = rows.Count > 0 ? rows[^1].Cursor : after,
            HasMore = hasMore,
            Changes = visible
                .GroupBy(change => change.RecordId)
                .Select(versions => versions.MaxBy(change => change.Cursor)!)
                .OrderBy(change => change.Cursor)
                .Select(ToMessage)
                .ToList(),
        };
    }

    private async Task<bool> AudienceChangedAsync(FeedDefinition feed, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (feed.Audience is not (FeedAudience.PermissionAudience or FeedAudience.EachRowAudience) || Caller.SessionId(user) is not { } session || Caller.UserId(user) is not { } userId)
            return false;

        GrantSet grants = await access.GrantsForAsync(userId, cancellationToken);
        string fingerprint = feed.Audience is FeedAudience.PermissionAudience audience
            ? Fingerprint(grants, grant => grant.Pattern.Matches(audience.Permission.Node))
            : Fingerprint(grants, MayDecideRoleMembership);
        AudienceMark? mark = await db.AudienceMarks.FindAsync([session, feed.Name], cancellationToken);
        bool changed = mark is not null && mark.Fingerprint != fingerprint;
        DateTime now = time.GetUtcNow().UtcDateTime;
        if (mark is not null && !changed && now - mark.SeenAt < AudienceMarkResolution)
            return false;

        if (mark is null)
            db.AudienceMarks.Add(mark = new AudienceMark { SessionId = session, Feed = feed.Name, Fingerprint = fingerprint });
        mark.Fingerprint = fingerprint;
        mark.SeenAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return changed;
    }

    private static bool MayDecideRoleMembership(Grant grant) =>
        grant.Pattern.Matches(AnyRoleMembership) || grant.Pattern.Text.TrimStart('-').StartsWith(RoleMembership.Prefix, StringComparison.Ordinal);

    private static string Fingerprint(GrantSet grants, Func<Grant, bool> counts) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', grants.Grants
            .Where(counts)
            .Select(grant => $"{grant.Pattern}@{grant.Scope}")
            .Order(StringComparer.Ordinal)))));

    private static Dictionary<string, long> CursorsOf(List<FeedCursor> cursors) =>
        cursors.GroupBy(cursor => cursor.Feed).ToDictionary(group => group.Key, group => group.Last().Cursor);

    private static FeedChange ToMessage(FeedChangeRecord row) => new()
    {
        Id = row.RecordId,
        Version = row.Version,
        Deleted = row.Deleted,
        Payload = row.Deleted ? [] : row.Payload,
        SortKey = row.SortKey,
        StartsAt = row.StartsAt,
        EndsAt = row.EndsAt,
        GroupKey = row.GroupKey,
    };

    private static ClaimsPrincipal UserOf(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User ?? new ClaimsPrincipal();
}
