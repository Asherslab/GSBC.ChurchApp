using GSBC.ChurchApp.Features.Notifications.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Notifications.Api.Push;

internal sealed class PushDispatcher(NotificationsDbContext db, IPushTransport transport, ILogger<PushDispatcher> logger)
{
    public const string InboxLink = "/notifications";
    private const int BatchSize = 100;
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan GiveUpAfter = TimeSpan.FromHours(1);
    private static readonly TimeSpan LongestBackoff = TimeSpan.FromMinutes(16);

    public async Task<int> DeliverDueAsync(DateTime now, CancellationToken cancellationToken)
    {
        DateTime leaseUntil = now + Lease;
        List<long> leased = await db.Database.SqlQuery<long>($"""
            UPDATE notifications.deliveries SET lease_until = {leaseUntil}
            WHERE id IN (
                SELECT id FROM notifications.deliveries
                WHERE sent_at IS NULL AND gave_up_at IS NULL AND due_at <= {now}
                  AND (lease_until IS NULL OR lease_until < {now})
                ORDER BY id LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED)
            RETURNING id AS "Value"
            """).ToListAsync(cancellationToken);
        if (leased.Count == 0)
            return 0;

        List<Delivery> due = await db.Deliveries.Include(delivery => delivery.Notification)
            .Where(delivery => leased.Contains(delivery.Id)).OrderBy(delivery => delivery.Id).ToListAsync(cancellationToken);
        List<Guid> deviceIds = due.Select(delivery => delivery.DeviceId).Distinct().ToList();
        Dictionary<Guid, Device> devices = await db.Devices.Where(device => deviceIds.Contains(device.Id)).ToDictionaryAsync(device => device.Id, cancellationToken);
        Dictionary<Guid, int> unread = await UnreadAsync(due, cancellationToken);
        HashSet<Guid> dead = [];

        foreach (IGrouping<(Guid Device, string? Group, bool Summary), Delivery> push in due.GroupBy(delivery =>
                     (delivery.DeviceId, delivery.Summarised ? delivery.PacingGroup : delivery.Id.ToString(), delivery.Summarised)))
        {
            List<Delivery> deliveries = push.ToList();
            if (dead.Contains(push.Key.Device) || !devices.TryGetValue(push.Key.Device, out Device? device))
                continue;

            PushResult result = await transport.SendAsync(
                new PushTarget(device.Platform, device.AppId, device.Environment, device.Token),
                Message(deliveries, device.UserId is { } userId ? unread.GetValueOrDefault(userId) : null),
                cancellationToken);
            Record(deliveries, result, now);
            if (result.Outcome == PushOutcome.DeadToken)
            {
                dead.Add(device.Id);
                db.Devices.Remove(device);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return due.Count;
    }

    private static PushMessage Message(List<Delivery> deliveries, int? badge)
    {
        Delivery first = deliveries[0];
        if (first.Kind == DeliveryKind.Background)
            return new PushMessage(null, null, null, null, null, null, null, Urgent: false, Background: true);

        NotificationRecord latest = deliveries.MaxBy(delivery => delivery.Id)!.Notification!;
        string thread = latest.ScopeId is null ? latest.Kind : $"{latest.Kind}|{latest.ScopeId}";
        if (deliveries.Count > 1)
        {
            return new PushMessage(latest.Area, $"{deliveries.Count} new · {latest.Title}", InboxLink, null, thread, thread, badge,
                Urgent: false, Background: false);
        }

        string title = latest.Test ? $"Test · {latest.Title}" : latest.Title;
        return new PushMessage(title, latest.Body, latest.Link ?? InboxLink, latest.Id.ToString(), thread, latest.Subject,
            badge, latest.Urgent, Background: false);
    }

    private void Record(List<Delivery> deliveries, PushResult result, DateTime now)
    {
        foreach (Delivery delivery in deliveries)
        {
            delivery.LeaseUntil = null;
            delivery.LastError = result.Error;
            switch (result.Outcome)
            {
                case PushOutcome.Sent:
                    delivery.SentAt = now;
                    break;
                case PushOutcome.Retry when now - delivery.CreatedAt < GiveUpAfter:
                    delivery.Attempts++;
                    delivery.DueAt = now + Backoff(delivery.Attempts);
                    break;
                default:
                    delivery.GaveUpAt = now;
                    break;
            }
        }

        if (result.Outcome is PushOutcome.Failed)
            logger.LogWarning("A push couldn't be sent: {Error}", result.Error);
    }

    private static TimeSpan Backoff(int attempts) =>
        TimeSpan.FromMinutes(Math.Min(Math.Pow(2, attempts - 1), LongestBackoff.TotalMinutes));

    private async Task<Dictionary<Guid, int>> UnreadAsync(List<Delivery> due, CancellationToken cancellationToken)
    {
        List<Guid> people = due.Where(delivery => delivery.UserId is not null).Select(delivery => delivery.UserId!.Value).Distinct().ToList();
        return await db.Inbox.AsNoTracking()
            .Where(entry => people.Contains(entry.UserId) && entry.ReadAt == null)
            .GroupBy(entry => entry.UserId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
    }
}
