using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class NotificationRaiser(
    NotificationsDbContext db, NotificationKindCatalog kinds, IPermissionHolders holders, IPeopleIndex people, PushSignal signal, TimeProvider time)
    : INotifications
{
    public async Task RaiseAsync(Notification notification, CancellationToken cancellationToken) => await RaisedAsync(notification, cancellationToken);

    public async Task<Guid> RaisedAsync(Notification notification, CancellationToken cancellationToken)
    {
        NotificationKind kind = NotificationRules.Validated(kinds, notification);
        DateTime now = time.GetUtcNow().UtcDateTime;
        Recipients recipients = await NotificationRules.RecipientsAsync(notification, holders, people, cancellationToken);

        NotificationRecord record = new()
        {
            Id = Guid.CreateVersion7(),
            Kind = kind.Key,
            Area = kind.Area,
            Title = notification.Title,
            Body = notification.Body,
            Link = notification.Link,
            Subject = notification.Subject,
            Urgent = notification.Urgent,
            ScopeId = notification.ScopeId,
            Test = notification.Test,
            Waits = kind.Waits,
            InAdminPanel = kind.InAdminPanel,
            Action = notification.Action,
            RaisedAt = now,
        };
        db.Notifications.Add(record);

        foreach (Guid person in recipients.People)
        {
            InboxEntry entry = new() { Id = Guid.CreateVersion7(), NotificationId = record.Id, UserId = person };
            db.Inbox.Add(entry);
            db.FeedChanges.Add(InboxPayloads.InboxRow(entry, record, version: 1));
        }
        if (recipients.Public)
            db.FeedChanges.Add(InboxPayloads.PublicRow(record));

        await QueuePushesAsync(kind, notification, record, recipients, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        signal.Wake();
        return record.Id;
    }

    public async Task SettleAsync(string subject, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<NotificationRecord> waiting = await db.Notifications
            .Where(record => record.Subject == subject && record.Waits && record.SettledAt == null)
            .ToListAsync(cancellationToken);
        if (waiting.Count == 0)
            return;

        List<Guid> notificationIds = waiting.Select(record => record.Id).ToList();
        List<InboxEntry> entries = await db.Inbox.Where(entry => notificationIds.Contains(entry.NotificationId)).ToListAsync(cancellationToken);
        List<string> rowIds = [.. entries.Select(entry => entry.Id.ToString()), .. notificationIds.Select(id => id.ToString())];
        Dictionary<(string Feed, string RecordId), long> versions = (await db.FeedChanges.AsNoTracking()
                .Where(change => (change.Feed == NotificationsFeeds.Inbox || change.Feed == NotificationsFeeds.Public) && rowIds.Contains(change.RecordId))
                .GroupBy(change => new { change.Feed, change.RecordId })
                .Select(group => new { group.Key.Feed, group.Key.RecordId, Version = group.Max(change => change.Version) })
                .ToListAsync(cancellationToken))
            .ToDictionary(row => (row.Feed, row.RecordId), row => row.Version);

        foreach (NotificationRecord record in waiting)
        {
            record.SettledAt = now;
            foreach (InboxEntry entry in entries.Where(entry => entry.NotificationId == record.Id))
                db.FeedChanges.Add(InboxPayloads.InboxRow(entry, record, versions.GetValueOrDefault((NotificationsFeeds.Inbox, entry.Id.ToString())) + 1));
            if (versions.TryGetValue((NotificationsFeeds.Public, record.Id.ToString()), out long publicVersion))
                db.FeedChanges.Add(InboxPayloads.PublicRow(record, publicVersion + 1));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(int People, int Phones)> ReachAsync(NotificationAudience audience, CancellationToken cancellationToken)
    {
        Recipients recipients = await NotificationRules.RecipientsAsync(
            new Notification(AnnouncementNotifications.Announcement, audience, "", ""), holders, people, cancellationToken);
        List<Device> devices = await DevicesAsync(recipients, cancellationToken);
        return (recipients.People.Count, devices.Count);
    }

    private async Task QueuePushesAsync(
        NotificationKind kind, Notification notification, NotificationRecord record, Recipients recipients, DateTime now, CancellationToken cancellationToken)
    {
        List<Guid> peopleIds = [.. recipients.People];
        List<Device> devices = await DevicesAsync(recipients, cancellationToken);
        if (devices.Count == 0)
            return;

        List<Guid> installIds = devices.Select(device => device.InstallId).ToList();
        List<Preference> preferences = await db.Preferences.AsNoTracking()
            .Where(preference => preference.Kind == kind.Key && (preference.ScopeId == null || preference.ScopeId == notification.ScopeId))
            .Where(preference => (preference.UserId != null && peopleIds.Contains(preference.UserId.Value))
                || (preference.InstallId != null && installIds.Contains(preference.InstallId.Value)))
            .ToListAsync(cancellationToken);

        string pacingGroup = $"{kind.Key}|{notification.ScopeId}";
        Dictionary<Guid, int> recent = kind.Pacing is { } pacing
            ? await RecentPushesAsync(devices.Select(device => device.Id).ToList(), pacingGroup, now - pacing.Per, cancellationToken)
            : [];

        foreach (Device device in devices)
        {
            if (!NotificationRules.PushWanted(kind, notification.ScopeId, device, preferences))
                continue;
            (bool summarised, DateTime due) = NotificationRules.Scheduled(kind, notification, recent.GetValueOrDefault(device.Id), now);
            db.Deliveries.Add(new Delivery
            {
                DeviceId = device.Id,
                NotificationId = record.Id,
                UserId = device.UserId,
                Kind = DeliveryKind.Visible,
                PacingGroup = pacingGroup,
                Summarised = summarised,
                CreatedAt = now,
                DueAt = due,
            });
        }
    }

    private Task<List<Device>> DevicesAsync(Recipients recipients, CancellationToken cancellationToken) =>
        db.Devices.AsNoTracking().Where(recipients.Devices()).ToListAsync(cancellationToken);

    private async Task<Dictionary<Guid, int>> RecentPushesAsync(List<Guid> deviceIds, string pacingGroup, DateTime since, CancellationToken cancellationToken) =>
        await db.Deliveries.AsNoTracking()
            .Where(delivery => deviceIds.Contains(delivery.DeviceId) && delivery.PacingGroup == pacingGroup && delivery.CreatedAt > since && !delivery.Summarised)
            .GroupBy(delivery => delivery.DeviceId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
}
