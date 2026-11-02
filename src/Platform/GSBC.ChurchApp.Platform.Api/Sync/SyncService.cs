using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Platform.Api.Sync;

public sealed class SyncService(
    SyncDbContext db, FeedRegistry feeds, IAccessEvaluator access, FeedChangeBroadcaster broadcaster) : ISyncService
{
    private const int PageSize = 500;
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AudienceMarkResolution = TimeSpan.FromDays(1);

    public async ValueTask<PullReply> PullAsync(PullRequest request, CallContext context = default)
    {
        Dictionary<string, long> cursors = CursorsOf(request.Cursors);
        return new PullReply { Feeds = await ReadAsync(cursors, cursors.Keys, UserOf(context), context.CancellationToken) };
    }

    public async IAsyncEnumerable<WatchEvent> WatchAsync(WatchRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        ClaimsPrincipal user = UserOf(context);
        Dictionary<string, long> cursors = CursorsOf(request.Cursors);
        using FeedChangeBroadcaster.Subscription subscription = broadcaster.Subscribe();

        IEnumerable<string> due = cursors.Keys;
        bool caughtUp = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            List<FeedBatch> batches = await ReadAsync(cursors, due, user, cancellationToken);
            if (batches.Count > 0 || !caughtUp)
                yield return new WatchEvent { Feeds = batches };
            caughtUp = true;

            if (batches.Any(batch => batch.HasMore))
            {
                due = batches.Where(batch => batch.HasMore).Select(batch => batch.Feed).ToList();
                continue;
            }

            ChangedFeeds? changed = await subscription.WaitAsync(HeartbeatInterval, cancellationToken);
            if (changed is null)
            {
                yield return new WatchEvent { IsHeartbeat = true };
                due = [];
            }
            else
            {
                due = cursors.Keys.Where(changed.Includes).ToList();
            }
        }
    }

    private async Task<List<FeedBatch>> ReadAsync(
        Dictionary<string, long> cursors, IEnumerable<string> due, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        List<FeedBatch> batches = [];
        Dictionary<string, long> horizons = await db.FeedHorizons.AsNoTracking()
            .ToDictionaryAsync(horizon => horizon.Feed, horizon => horizon.Cursor, cancellationToken);

        foreach (string name in due)
        {
            if (!feeds.ByName.TryGetValue(name, out FeedDefinition? feed))
                continue;

            bool behindHorizon = cursors[name] > 0 && cursors[name] < horizons.GetValueOrDefault(name);
            bool audienceChanged = await AudienceChangedAsync(feed, user, cancellationToken) && cursors[name] > 0;
            FeedBatch batch;
            if (!await CanSeeFeedAsync(feed, user, cancellationToken))
            {
                batch = new FeedBatch { Feed = feed.Name, Reset = true };
            }
            else if (behindHorizon || audienceChanged)
            {
                batch = new FeedBatch { Feed = feed.Name, Reset = true, Cursor = 0, HasMore = true };
            }
            else
            {
                batch = await ReadBatchAsync(feed, cursors[name], user, cancellationToken);
            }

            if (batch.Reset || batch.Cursor != cursors[name])
            {
                cursors[name] = batch.Cursor;
                batches.Add(batch);
            }
        }

        return batches;
    }

    private async Task<FeedBatch> ReadBatchAsync(FeedDefinition feed, long after, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        List<FeedChangeRecord> rows = await db.FeedChanges
            .AsNoTracking()
            .Where(change => change.Feed == feed.Name && change.Cursor > after)
            .OrderBy(change => change.Cursor)
            .Take(PageSize + 1)
            .ToListAsync(cancellationToken);

        bool hasMore = rows.Count > PageSize;
        if (hasMore)
            rows.RemoveAt(PageSize);

        List<FeedChangeRecord> visible = await VisibleRowsAsync(feed, rows, user, cancellationToken);

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

    private async Task<List<FeedChangeRecord>> VisibleRowsAsync(
        FeedDefinition feed, List<FeedChangeRecord> rows, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (feed.Audience is not FeedAudience.PermissionAudience audience)
            return rows;

        Dictionary<string, bool> allowedByScope = [];
        foreach (string? scopeId in rows.Select(row => row.ScopeId).Distinct())
            allowedByScope[scopeId ?? ""] = await access.IsAllowedAsync(user, audience.Permission.Node, scopeId, cancellationToken);

        return rows.Where(row => allowedByScope[row.ScopeId ?? ""]).ToList();
    }

    private async Task<bool> AudienceChangedAsync(FeedDefinition feed, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (feed.Audience is not FeedAudience.PermissionAudience audience || Caller.SessionId(user) is not { } session || Caller.UserId(user) is not { } userId)
            return false;

        string fingerprint = Fingerprint(await access.GrantsForAsync(userId, cancellationToken), audience.Permission.Node);
        AudienceMark? mark = await db.AudienceMarks.FindAsync([session, feed.Name], cancellationToken);
        bool changed = mark is not null && mark.Fingerprint != fingerprint;
        DateTime now = DateTime.UtcNow;
        if (mark is not null && !changed && now - mark.SeenAt < AudienceMarkResolution)
            return false;

        if (mark is null)
            db.AudienceMarks.Add(mark = new AudienceMark { SessionId = session, Feed = feed.Name, Fingerprint = fingerprint });
        mark.Fingerprint = fingerprint;
        mark.SeenAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return changed;
    }

    private static string Fingerprint(GrantSet grants, string node) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', grants.Grants
            .Where(grant => grant.Pattern.Matches(node))
            .Select(grant => $"{grant.Pattern}@{grant.Scope}")
            .Order(StringComparer.Ordinal)))));

    private async Task<bool> CanSeeFeedAsync(FeedDefinition feed, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        feed.Audience switch
        {
            FeedAudience.PublicAudience => true,
            FeedAudience.SignedInAudience => IsSignedIn(user),
            FeedAudience.PermissionAudience audience when audience.Permission.ScopeKind == ScopeKind.Global =>
                IsSignedIn(user) && await access.IsAllowedAsync(user, audience.Permission.Node, null, cancellationToken),
            FeedAudience.PermissionAudience => IsSignedIn(user),
            _ => false,
        };

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

    private static bool IsSignedIn(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true;
}
