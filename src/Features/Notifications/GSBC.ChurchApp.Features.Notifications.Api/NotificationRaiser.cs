using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class NotificationRaiser(
    NotificationsDbContext db, NotificationKindCatalog kinds, IPermissionHolders holders, IPeopleIndex people, PushSignal signal, TimeProvider time)
    : INotifications
{
    private static readonly TimeSpan SummaryPause = TimeSpan.FromMinutes(10);

    public async Task RaiseAsync(Notification notification, CancellationToken cancellationToken)
    {
        NotificationKind kind = Validated(notification);
        DateTime now = time.GetUtcNow().UtcDateTime;
        Recipients recipients = await RecipientsAsync(notification, cancellationToken);

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
    }

    public async Task<(int People, int Phones)> ReachAsync(NotificationAudience audience, CancellationToken cancellationToken)
    {
        Recipients recipients = await RecipientsAsync(new Notification(AnnouncementNotifications.Announcement, audience, "", ""), cancellationToken);
        List<Device> devices = await DevicesAsync(recipients, cancellationToken);
        return (recipients.People.Count, devices.Count);
    }

    private NotificationKind Validated(Notification notification)
    {
        if (!kinds.ByKey.TryGetValue(notification.Kind.Key, out NotificationKind? kind))
            throw new InvalidOperationException($"The notification kind '{notification.Kind.Key}' isn't registered.");
        if (notification.Subject is not null && LinkTargets.InAppRoute(notification.Link ?? "") is null)
            throw new ArgumentException($"A notification about '{notification.Subject}' must link to it in the app.", nameof(notification));
        if (notification.Link is { } link && !LinkTargets.IsSafe(link))
            throw new ArgumentException($"'{link}' isn't a safe link.", nameof(notification));
        if (notification.Urgent && !kind.MayBeUrgent)
            throw new ArgumentException($"'{kind.Key}' notifications can't be urgent.", nameof(notification));
        return kind;
    }

    private async Task<Recipients> RecipientsAsync(Notification notification, CancellationToken cancellationToken)
    {
        if (notification.Test && notification.Audience is not NotificationAudience.PersonAudience)
            throw new ArgumentException("A test goes to one person.", nameof(notification));

        switch (notification.Audience)
        {
            case NotificationAudience.EveryoneAudience:
                return new Recipients(await people.EveryoneAsync(cancellationToken), [], EveryPhone: true, Public: true);
            case NotificationAudience.SignedInAudience:
                return new Recipients(await people.EveryoneAsync(cancellationToken), [], EveryPhone: false, Public: false);
            case NotificationAudience.PersonAudience person:
                return new Recipients([person.UserId], [], EveryPhone: false, Public: false);
            case NotificationAudience.HoldingAudience holding:
                PermissionHolders found = await holders.HoldersAsync(holding.Permission, holding.ObjectId, cancellationToken);
                return new Recipients(found.People, found.Phones, EveryPhone: false, Public: found.Phones.Count > 0);
            default:
                throw new ArgumentException("Unknown audience.", nameof(notification));
        }
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
        bool interrupts = notification.Urgent || kind.Mandatory || notification.Test;

        foreach (Device device in devices)
        {
            if (!PushWanted(kind, notification.ScopeId, device, preferences))
                continue;
            bool overLimit = kind.Pacing is { } limit && recent.GetValueOrDefault(device.Id) >= limit.Pushes;
            DateTime due = overLimit ? now + SummaryPause : now;
            db.Deliveries.Add(new Delivery
            {
                DeviceId = device.Id,
                NotificationId = record.Id,
                UserId = device.UserId,
                Kind = DeliveryKind.Visible,
                PacingGroup = pacingGroup,
                Summarised = overLimit,
                CreatedAt = now,
                DueAt = interrupts ? due : QuietHours.NextSendable(due),
            });
        }
    }

    private async Task<List<Device>> DevicesAsync(Recipients recipients, CancellationToken cancellationToken)
    {
        List<Guid> peopleIds = [.. recipients.People];
        List<Guid> phoneIds = [.. recipients.Phones];
        return await db.Devices.AsNoTracking()
            .Where(device => (device.UserId != null && peopleIds.Contains(device.UserId.Value))
                || (device.UserId == null && (recipients.EveryPhone || phoneIds.Contains(device.InstallId))))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> RecentPushesAsync(List<Guid> deviceIds, string pacingGroup, DateTime since, CancellationToken cancellationToken) =>
        await db.Deliveries.AsNoTracking()
            .Where(delivery => deviceIds.Contains(delivery.DeviceId) && delivery.PacingGroup == pacingGroup && delivery.CreatedAt > since && !delivery.Summarised)
            .GroupBy(delivery => delivery.DeviceId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);

    private static bool PushWanted(NotificationKind kind, string? scopeId, Device device, List<Preference> preferences)
    {
        if (kind.Mandatory)
            return true;
        IEnumerable<Preference> mine = device.UserId is { } userId
            ? preferences.Where(preference => preference.UserId == userId)
            : preferences.Where(preference => preference.InstallId == device.InstallId);
        List<Preference> list = mine.ToList();
        Preference? chosen = list.FirstOrDefault(preference => scopeId is not null && preference.ScopeId == scopeId)
            ?? list.FirstOrDefault(preference => preference.ScopeId is null);
        return chosen?.Push ?? kind.DefaultPush;
    }

    private sealed record Recipients(IReadOnlyList<Guid> People, IReadOnlyList<Guid> Phones, bool EveryPhone, bool Public);
}
