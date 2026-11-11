using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Api.Push;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class NotificationRaiser(
    NotificationsDbContext db, NotificationKindCatalog kinds, DigestSchedules schedules, IPermissionHolders holders, IPeopleIndex people, PushSignal signal,
    TimeProvider time)
    : INotifications
{
    public async Task RaiseAsync(Notification notification, CancellationToken cancellationToken) => await RaisedAsync(notification, cancellationToken);

    public async Task RaiseOneOfAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken) =>
        await RaisedAsync(mostSpecificFirst, cancellationToken);

    public async Task<Guid> RaisedAsync(Notification notification, CancellationToken cancellationToken) =>
        (await RaisedAsync([notification], cancellationToken))[0];

    public async Task RaiseToAsync(Notification digest, Recipients recipients, CancellationToken cancellationToken) =>
        await RaisedAsync([digest], cancellationToken, recipients);

    public async Task CollectAsync(NotificationDigestItem item, CancellationToken cancellationToken)
    {
        NotificationKind kind = schedules.Kind(item.Kind.Key);
        NotificationLinks.Checked(new Notification(kind, item.Audience, "", "", item.Link) { Actor = item.Actor });
        DateTime now = time.GetUtcNow().UtcDateTime;
        DigestRecord fields = new() { Kind = kind.Key, GroupKey = item.Group, AudienceKind = "", Link = item.Link, ScopeId = item.ScopeId, Action = item.Action };
        DigestAudiences.Write(item.Audience, fields);

        StartedDigest digest = (await db.Database.SqlQuery<StartedDigest>($"""
            INSERT INTO notifications.digests (id, kind, group_key, audience_kind, audience_user_id, audience_node, audience_object_id, link, scope_id, action, started_at, sent_through)
            VALUES ({Guid.CreateVersion7()}, {fields.Kind}, {fields.GroupKey}, {fields.AudienceKind}, {fields.AudienceUserId}, {fields.AudienceNode}, {fields.AudienceObjectId},
                    {fields.Link}, {fields.ScopeId}, {fields.Action}, {item.StartedAt ?? now}, 0)
            ON CONFLICT (kind, group_key) DO UPDATE SET audience_kind = excluded.audience_kind, audience_user_id = excluded.audience_user_id,
                audience_node = excluded.audience_node, audience_object_id = excluded.audience_object_id, link = excluded.link, scope_id = excluded.scope_id,
                action = excluded.action
            RETURNING id, started_at
            """).ToListAsync(cancellationToken)).Single();
        db.DigestItems.Add(new DigestItemRecord { DigestId = digest.Id, ActorId = item.Actor, At = now });
        await db.SaveChangesAsync(cancellationToken);

        if (DigestTimes.Next(await schedules.OfAsync(kind, cancellationToken), digest.StartedAt, now) is { } due)
            await db.Database.ExecuteSqlAsync($"UPDATE notifications.digests SET due_at = {due} WHERE id = {digest.Id} AND due_at IS NULL", cancellationToken);
    }

    public async Task ForgetDigestAsync(NotificationKind kind, string group, CancellationToken cancellationToken) =>
        await db.Digests.Where(digest => digest.Kind == kind.Key && digest.GroupKey == group).ExecuteDeleteAsync(cancellationToken);

    private async Task<IReadOnlyList<Guid>> RaisedAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken, Recipients? only = null)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        Guid? oneOf = mostSpecificFirst.Count > 1 ? Guid.CreateVersion7() : null;
        List<Raised> raised = [];
        foreach ((int rank, Notification notification) in mostSpecificFirst.Index())
        {
            NotificationKind kind = NotificationRules.Validated(kinds, notification);
            if (kind.Digest is not null && only is null)
                throw new InvalidOperationException($"'{kind.Key}' notifications come as a digest: collect them with CollectAsync.");
            Recipients recipients = only ?? await NotificationRules.RecipientsAsync(notification, holders, people, cancellationToken);
            NotificationRecord record = RecordOf(kind, notification, now, oneOf, rank);
            db.Notifications.Add(record);
            if (recipients.Public)
                db.FeedChanges.Add(InboxPayloads.PublicRow(record));
            raised.Add(new Raised(kind, notification, record, recipients));
        }

        Recipients everyone = Recipients.Together(raised.Select(each => each.Recipients).ToList());
        List<Device> devices = await DevicesAsync(everyone, cancellationToken);
        List<Preference> preferences = await PreferencesAsync(raised, everyone, devices, cancellationToken);

        foreach (Guid person in everyone.People)
        {
            if (OneNotification.Choose(raised, each => each.Recipients.Reach(person), each => each.WantedBy(person, preferences)) is not { } chosen)
                continue;
            InboxEntry entry = new() { Id = Guid.CreateVersion7(), NotificationId = chosen.Record.Id, UserId = person };
            db.Inbox.Add(entry);
            db.FeedChanges.Add(InboxPayloads.InboxRow(entry, chosen.Record, version: 1));
        }

        await QueuePushesAsync(raised, devices, preferences, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        signal.Wake();
        return raised.Select(each => each.Record.Id).ToList();
    }

    public async Task SettleAsync(string subject, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<NotificationRecord> waiting = await db.Notifications
            .Where(record => record.Subject == subject && record.Waits && record.SettledAt == null && record.RecalledAt == null)
            .ToListAsync(cancellationToken);
        if (waiting.Count == 0)
            return;

        List<Guid> notificationIds = waiting.Select(record => record.Id).ToList();
        List<InboxEntry> entries = await db.Inbox.Where(entry => notificationIds.Contains(entry.NotificationId)).ToListAsync(cancellationToken);
        Dictionary<(string Feed, string RecordId), long> versions = await RowVersionsAsync(entries, notificationIds, cancellationToken);

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

    public async Task RecallAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        NotificationRecord record = await db.Notifications.SingleAsync(candidate => candidate.Id == notificationId, cancellationToken);
        if (record.RecalledAt is not null)
            return;

        List<InboxEntry> entries = await db.Inbox.Where(entry => entry.NotificationId == notificationId).ToListAsync(cancellationToken);
        Dictionary<(string Feed, string RecordId), long> versions = await RowVersionsAsync(entries, [notificationId], cancellationToken);
        record.RecalledAt = now;
        foreach (InboxEntry entry in entries)
        {
            db.FeedChanges.Add(InboxPayloads.Gone(NotificationsFeeds.Inbox, entry.Id.ToString(), entry.UserId.ToString(),
                versions.GetValueOrDefault((NotificationsFeeds.Inbox, entry.Id.ToString())) + 1));
        }
        if (versions.TryGetValue((NotificationsFeeds.Public, notificationId.ToString()), out long publicVersion))
            db.FeedChanges.Add(InboxPayloads.Gone(NotificationsFeeds.Public, notificationId.ToString(), null, publicVersion + 1));
        db.Inbox.RemoveRange(entries);
        await db.Deliveries.Where(delivery => delivery.NotificationId == notificationId && delivery.SentAt == null && delivery.GaveUpAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(delivery => delivery.GaveUpAt, now).SetProperty(delivery => delivery.LastError, PushResult.Recalled.Error), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (await SyncNudger.NudgeAsync(db, now, cancellationToken) > 0)
            signal.Wake();
    }

    public async Task<(int People, int Phones)> ReachAsync(NotificationAudience audience, CancellationToken cancellationToken)
    {
        Recipients recipients = await NotificationRules.RecipientsAsync(
            new Notification(AnnouncementNotifications.Announcement, audience, "", "") { Actor = null }, holders, people, cancellationToken);
        List<Device> devices = await DevicesAsync(recipients, cancellationToken);
        return (recipients.People.Count, devices.Count);
    }

    private async Task<Dictionary<(string Feed, string RecordId), long>> RowVersionsAsync(
        List<InboxEntry> entries, List<Guid> notificationIds, CancellationToken cancellationToken)
    {
        List<string> rowIds = [.. entries.Select(entry => entry.Id.ToString()), .. notificationIds.Select(id => id.ToString())];
        return (await db.FeedChanges.AsNoTracking()
                .Where(change => (change.Feed == NotificationsFeeds.Inbox || change.Feed == NotificationsFeeds.Public) && rowIds.Contains(change.RecordId))
                .GroupBy(change => new { change.Feed, change.RecordId })
                .Select(group => new { group.Key.Feed, group.Key.RecordId, Version = group.Max(change => change.Version) })
                .ToListAsync(cancellationToken))
            .ToDictionary(row => (row.Feed, row.RecordId), row => row.Version);
    }

    private async Task QueuePushesAsync(
        List<Raised> raised, List<Device> devices, List<Preference> preferences, DateTime now, CancellationToken cancellationToken)
    {
        Dictionary<string, Dictionary<Guid, int>> recent = [];
        foreach (Raised each in raised.Where(each => each.Kind.Pacing is not null).DistinctBy(each => each.PacingGroup))
            recent[each.PacingGroup] = await RecentPushesAsync(devices.Select(device => device.Id).ToList(), each.PacingGroup, now - each.Kind.Pacing!.Per, cancellationToken);

        foreach (Device device in devices)
        {
            if (OneNotification.Choose(raised, each => each.Recipients.Reach(device), each => each.WantedOn(device, preferences)) is not { } chosen)
                continue;
            int recentPushes = recent.GetValueOrDefault(chosen.PacingGroup)?.GetValueOrDefault(device.Id) ?? 0;
            (bool summarised, DateTime due) = NotificationRules.Scheduled(chosen.Kind, chosen.Notification, recentPushes, now);
            db.Deliveries.Add(new Delivery
            {
                DeviceId = device.Id,
                NotificationId = chosen.Record.Id,
                UserId = device.UserId,
                Kind = DeliveryKind.Visible,
                PacingGroup = chosen.PacingGroup,
                Summarised = summarised,
                CreatedAt = now,
                DueAt = due,
            });
        }
    }

    private async Task<List<Preference>> PreferencesAsync(List<Raised> raised, Recipients everyone, List<Device> devices, CancellationToken cancellationToken)
    {
        List<string> kindKeys = raised.Select(each => each.Kind.Key).Distinct().ToList();
        List<Guid> peopleIds = [.. everyone.People];
        List<Guid> installIds = devices.Where(device => device.UserId is null).Select(device => device.InstallId).ToList();
        return await db.Preferences.AsNoTracking()
            .Where(preference => kindKeys.Contains(preference.Kind))
            .Where(preference => (preference.UserId != null && peopleIds.Contains(preference.UserId.Value))
                || (preference.InstallId != null && installIds.Contains(preference.InstallId.Value)))
            .ToListAsync(cancellationToken);
    }

    private static NotificationRecord RecordOf(NotificationKind kind, Notification notification, DateTime now, Guid? oneOf, int rank) => new()
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
        OneOf = oneOf,
        Rank = rank,
    };

    private Task<List<Device>> DevicesAsync(Recipients recipients, CancellationToken cancellationToken) =>
        db.Devices.AsNoTracking().Where(recipients.Devices()).ToListAsync(cancellationToken);

    private async Task<Dictionary<Guid, int>> RecentPushesAsync(List<Guid> deviceIds, string pacingGroup, DateTime since, CancellationToken cancellationToken) =>
        await db.Deliveries.AsNoTracking()
            .Where(delivery => deviceIds.Contains(delivery.DeviceId) && delivery.PacingGroup == pacingGroup && delivery.CreatedAt > since && !delivery.Summarised)
            .GroupBy(delivery => delivery.DeviceId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);

    private sealed record StartedDigest(Guid Id, DateTime StartedAt);

    private sealed record Raised(NotificationKind Kind, Notification Notification, NotificationRecord Record, Recipients Recipients)
    {
        public string PacingGroup => $"{Kind.Key}|{Notification.ScopeId}";

        public bool WantedBy(Guid person, IReadOnlyCollection<Preference> preferences) =>
            Notification.Test || NotificationRules.WantedBy(Kind, Notification.ScopeId, person, preferences);

        public bool WantedOn(Device device, IReadOnlyCollection<Preference> preferences) =>
            Notification.Test || NotificationRules.Wanted(Kind, Notification.ScopeId, device, preferences);
    }
}
