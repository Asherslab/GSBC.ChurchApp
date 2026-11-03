using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class KindsFeed(IServiceScopeFactory scopes, NotificationKindCatalog kinds, ILogger<KindsFeed> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            await PublishAsync(scope.ServiceProvider.GetRequiredService<NotificationsDbContext>(), kinds, stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Publishing the notification kinds failed");
        }
    }

    public static async Task PublishAsync(NotificationsDbContext db, NotificationKindCatalog kinds, CancellationToken cancellationToken)
    {
        Dictionary<string, FeedChangeRecord> published = (await db.FeedChanges.AsNoTracking()
                .Where(change => change.Feed == NotificationsFeeds.Kinds)
                .GroupBy(change => change.RecordId)
                .Select(versions => versions.OrderByDescending(change => change.Cursor).First())
                .ToListAsync(cancellationToken))
            .ToDictionary(change => change.RecordId);

        foreach (NotificationKind kind in kinds.ByKey.Values)
        {
            byte[] payload = InboxPayloads.Encode(new KindInfo
            {
                Key = kind.Key,
                Description = kind.Description,
                Area = kind.Area,
                DefaultPush = kind.DefaultPush,
                Mandatory = kind.Mandatory,
                PerScope = kind.PerScope?.Name,
            });
            FeedChangeRecord? before = published.GetValueOrDefault(kind.Key);
            if (before is { Deleted: false } && before.Payload.AsSpan().SequenceEqual(payload))
                continue;
            db.FeedChanges.Add(new FeedChangeRecord
            {
                Feed = NotificationsFeeds.Kinds, RecordId = kind.Key, Version = (before?.Version ?? 0) + 1, SortKey = $"{kind.Area}|{kind.Key}", Payload = payload,
            });
        }

        foreach (FeedChangeRecord gone in published.Values.Where(change => !change.Deleted && !kinds.ByKey.ContainsKey(change.RecordId)))
            db.FeedChanges.Add(new FeedChangeRecord { Feed = NotificationsFeeds.Kinds, RecordId = gone.RecordId, Version = gone.Version + 1, Deleted = true });

        await db.SaveChangesAsync(cancellationToken);
    }
}
