using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Platform.Api.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Notifications.Api.Push;

internal sealed class SyncNudger(
    FeedChangeBroadcaster broadcaster, IServiceScopeFactory scopes, PushSignal signal, TimeProvider time, ILogger<SyncNudger> logger) : BackgroundService
{
    public static readonly TimeSpan PerDevice = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan Gather = TimeSpan.FromSeconds(10);
    private const string OwnFeedsPrefix = "notifications.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using FeedChangeBroadcaster.Subscription changes = broadcaster.Subscribe();
        while (!stoppingToken.IsCancellationRequested)
        {
            ChangedFeeds? changed = await changes.WaitAsync(Timeout.InfiniteTimeSpan, stoppingToken);
            if (changed is null || (!changed.All && changed.Names.All(feed => feed.StartsWith(OwnFeedsPrefix, StringComparison.Ordinal))))
                continue;

            await Task.Delay(Gather, time, stoppingToken);
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                if (await NudgeAsync(scope.ServiceProvider.GetRequiredService<NotificationsDbContext>(), time.GetUtcNow().UtcDateTime, stoppingToken) > 0)
                    signal.Wake();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Queuing sync pushes failed");
            }
        }
    }

    public static async Task<int> NudgeAsync(NotificationsDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        DateTime before = now - PerDevice;
        List<Guid> devices = await db.Database.SqlQuery<Guid>($"""
            UPDATE notifications.devices SET last_nudged_at = {now}
            WHERE last_nudged_at IS NULL OR last_nudged_at < {before}
            RETURNING id AS "Value"
            """).ToListAsync(cancellationToken);
        foreach (Guid device in devices)
        {
            db.Deliveries.Add(new Delivery
            {
                DeviceId = device,
                Kind = DeliveryKind.Background,
                CreatedAt = now,
                DueAt = now,
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        return devices.Count;
    }
}
