using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Platform.Api.Sync;

public sealed class SyncService(SyncDbContext db, FeedRegistry feeds, IAccessEvaluator access) : ISyncService
{
    private const int PageSize = 500;
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

    public async ValueTask<PullReply> PullAsync(PullRequest request, CallContext context = default) =>
        new() { Feeds = await CatchUpAsync(request.Cursors, UserOf(context), context.CancellationToken) };

    public async IAsyncEnumerable<WatchEvent> WatchAsync(WatchRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        yield return new WatchEvent { Feeds = await CatchUpAsync(request.Cursors, UserOf(context), cancellationToken) };

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!await DelayAsync(HeartbeatInterval, cancellationToken))
                yield break;
            yield return new WatchEvent { IsHeartbeat = true };
        }
    }

    private async Task<List<FeedBatch>> CatchUpAsync(List<FeedCursor> cursors, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        List<FeedBatch> batches = [];
        foreach (FeedCursor cursor in cursors)
        {
            if (!feeds.ByName.TryGetValue(cursor.Feed, out FeedDefinition? feed))
                continue;

            batches.Add(await CanSeeFeedAsync(feed, user, cancellationToken)
                ? await ReadBatchAsync(feed, cursor.Cursor, user, cancellationToken)
                : new FeedBatch { Feed = feed.Name, Reset = true });
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
            Changes = visible.Select(ToMessage).ToList(),
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

    private static async Task<bool> DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
