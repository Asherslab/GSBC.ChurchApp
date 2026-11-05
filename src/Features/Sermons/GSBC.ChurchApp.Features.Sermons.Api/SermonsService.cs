using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Installs;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SermonsService(SermonsDbContext db, SermonFollowing following, TimeProvider time) : ISermonsService
{
    public const int BatchLimit = 500;
    public static readonly TimeSpan ClockSlack = TimeSpan.FromMinutes(5);

    public const int MostLatest = 10;

    public async ValueTask<SermonPage> LatestAsync(LatestQuery request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        List<SermonRecord> latest = await db.Sermons.AsNoTracking().Where(sermon => sermon.PublishedHash != null)
            .OrderByDescending(sermon => sermon.PreachedAt).ThenByDescending(sermon => sermon.AddedAt)
            .Take(Math.Clamp(request.Count, 1, MostLatest)).ToListAsync(cancellationToken);
        Dictionary<Guid, SeriesRecord> series = await db.Series.AsNoTracking().ToDictionaryAsync(record => record.Id, cancellationToken);
        Dictionary<Guid, SpeakerRecord> speakers = await db.Speakers.AsNoTracking().ToDictionaryAsync(record => record.Id, cancellationToken);
        return new SermonPage { Sermons = latest.Select(sermon => SermonPublisher.Payload(sermon, series, speakers)).ToList() };
    }

    public async ValueTask<Follows> FollowAsync(FollowRequest request, CallContext context = default) =>
        await following.FollowAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);

    public async ValueTask<Follows> FollowsAsync(Done request, CallContext context = default) =>
        await following.FollowsAsync(CallingInstall.Of(context), CallerId(context), context.CancellationToken);

    private static Guid? CallerId(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User is { } user ? Caller.UserId(user) : null;

    public async ValueTask<Done> SaveProgressAsync(ProgressBatch request, CallContext context = default)
    {
        Guid userId = Caller.UserId(context.ServerCallContext!.GetHttpContext().User)
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));
        await SaveAsync(userId, request.Entries, context.CancellationToken);
        return new Done();
    }

    public async Task SaveAsync(Guid userId, IReadOnlyList<ListeningEntry> entries, CancellationToken cancellationToken)
    {
        if (entries.Count > BatchLimit)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Send at most {BatchLimit} at a time."));

        DateTime now = time.GetUtcNow().UtcDateTime;
        Dictionary<Guid, ListeningEntry> incoming = [];
        foreach (ListeningEntry entry in entries)
        {
            if (!Guid.TryParse(entry.SermonId, out Guid sermonId) || !double.IsFinite(entry.Position) || !double.IsFinite(entry.Duration))
                continue;
            ListeningEntry clamped = Clamped(entry, now);
            incoming[sermonId] = incoming.TryGetValue(sermonId, out ListeningEntry? earlier) ? ListeningProgress.Merge(earlier, clamped) : clamped;
        }

        List<Guid> ids = incoming.Keys.ToList();
        HashSet<Guid> known = (await db.Sermons.Where(sermon => ids.Contains(sermon.Id)).Select(sermon => sermon.Id).ToListAsync(cancellationToken)).ToHashSet();
        Dictionary<Guid, ProgressRecord> stored = await db.Progress.Where(progress => progress.UserId == userId && ids.Contains(progress.SermonId))
            .ToDictionaryAsync(progress => progress.SermonId, cancellationToken);

        foreach ((Guid sermonId, ListeningEntry entry) in incoming.Where(pair => known.Contains(pair.Key)))
        {
            ListeningEntry? before = stored.TryGetValue(sermonId, out ProgressRecord? record) ? Entry(record) : null;
            ListeningEntry merged = ListeningProgress.MergeEither(before, entry)!;
            if (before is not null && Same(before, merged))
                continue;
            if (record is null)
            {
                record = new ProgressRecord { UserId = userId, SermonId = sermonId };
                db.Progress.Add(record);
            }
            Apply(record, merged, now);
            db.FeedChanges.Add(Row(record));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public static FeedChangeRecord Row(ProgressRecord record) => new()
    {
        Feed = SermonsFeeds.Progress,
        RecordId = $"{record.UserId:N}-{record.SermonId:N}",
        ScopeId = record.UserId.ToString(),
        Version = record.UpdatedAt.Ticks,
        SortKey = record.HeardAt?.ToString("O"),
        Payload = SermonsPayloads.Encode(Entry(record)),
    };

    public static ListeningEntry Entry(ProgressRecord record) => new()
    {
        SermonId = record.SermonId.ToString(),
        Position = record.Position,
        Duration = record.Duration,
        HeardAt = record.HeardAt,
        FinishedAt = record.FinishedAt,
        WatchedAt = record.WatchedAt,
        ClearedAt = record.ClearedAt,
    };

    private static void Apply(ProgressRecord record, ListeningEntry entry, DateTime now)
    {
        record.Position = Math.Max(0, entry.Position);
        record.Duration = Math.Max(0, entry.Duration);
        record.HeardAt = entry.HeardAt;
        record.FinishedAt = entry.FinishedAt;
        record.WatchedAt = entry.WatchedAt;
        record.ClearedAt = entry.ClearedAt;
        record.UpdatedAt = now > record.UpdatedAt ? now : record.UpdatedAt.AddTicks(1);
    }

    private static ListeningEntry Clamped(ListeningEntry entry, DateTime now)
    {
        DateTime latest = now + ClockSlack;
        DateTime? Clamp(DateTime? at) => at is { } value ? (value.ToUniversalTime() > latest ? latest : value.ToUniversalTime()) : null;
        return new ListeningEntry
        {
            SermonId = entry.SermonId,
            Position = entry.Position,
            Duration = entry.Duration,
            HeardAt = Clamp(entry.HeardAt),
            FinishedAt = Clamp(entry.FinishedAt),
            WatchedAt = Clamp(entry.WatchedAt),
            ClearedAt = Clamp(entry.ClearedAt),
        };
    }

    private static bool Same(ListeningEntry first, ListeningEntry second) =>
        first.Position.Equals(second.Position) && first.Duration.Equals(second.Duration) && first.HeardAt == second.HeardAt
        && first.FinishedAt == second.FinishedAt && first.WatchedAt == second.WatchedAt && first.ClearedAt == second.ClearedAt;
}
