using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class DigestSchedules(NotificationsDbContext db, NotificationKindCatalog kinds, TimeProvider time)
{
    public async Task<DigestSchedule> OfAsync(NotificationKind kind, CancellationToken cancellationToken) =>
        await db.DigestSchedules.AsNoTracking().SingleOrDefaultAsync(schedule => schedule.Kind == kind.Key, cancellationToken) is { } chosen
            ? new DigestSchedule(chosen.AfterMinutes.Select(minutes => TimeSpan.FromMinutes(minutes)).ToList(), chosen.DailyAt)
            : DigestOf(kind).Schedule;

    public async Task ChangeAsync(NotificationKind kind, DigestSchedule schedule, Guid? by, CancellationToken cancellationToken)
    {
        DigestScheduleRecord? record = await db.DigestSchedules.SingleOrDefaultAsync(chosen => chosen.Kind == kind.Key, cancellationToken);
        if (schedule == DigestOf(kind).Schedule)
        {
            if (record is not null)
                db.DigestSchedules.Remove(record);
        }
        else
        {
            record ??= db.DigestSchedules.Add(new DigestScheduleRecord { Kind = kind.Key }).Entity;
            record.AfterMinutes = schedule.After.Select(after => (int)after.TotalMinutes).ToArray();
            record.DailyAt = schedule.DailyAt;
            record.ChangedAt = time.GetUtcNow().UtcDateTime;
            record.ChangedBy = by;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public NotificationKind Kind(string key) =>
        kinds.ByKey.TryGetValue(key, out NotificationKind? kind) && kind.Digest is not null
            ? kind
            : throw new ArgumentException($"'{key}' isn't a notification that comes as a digest.", nameof(key));

    public static NotificationDigest DigestOf(NotificationKind kind) =>
        kind.Digest ?? throw new ArgumentException($"'{kind.Key}' notifications don't come as a digest.", nameof(kind));
}

internal static class DigestAudiences
{
    private const string Everyone = "everyone";
    private const string SignedIn = "signed-in";
    private const string Person = "person";
    private const string Holding = "holding";

    public static void Write(NotificationAudience audience, DigestRecord digest)
    {
        (digest.AudienceKind, digest.AudienceUserId, digest.AudienceNode, digest.AudienceObjectId) = audience switch
        {
            NotificationAudience.EveryoneAudience => (Everyone, (Guid?)null, (string?)null, (string?)null),
            NotificationAudience.SignedInAudience => (SignedIn, null, null, null),
            NotificationAudience.PersonAudience person => (Person, person.UserId, null, null),
            NotificationAudience.HoldingAudience holding => (Holding, null, holding.Permission.Node, holding.ObjectId),
            _ => throw new ArgumentOutOfRangeException(nameof(audience)),
        };
    }

    public static NotificationAudience? Read(DigestRecord digest, PermissionCatalog permissions) => digest.AudienceKind switch
    {
        Everyone => NotificationAudience.Everyone,
        SignedIn => NotificationAudience.SignedIn,
        Person when digest.AudienceUserId is { } person => NotificationAudience.Person(person),
        Holding when digest.AudienceNode is { } node && permissions.ByNode.TryGetValue(node, out Permission? permission) =>
            NotificationAudience.Holding(permission, digest.AudienceObjectId),
        _ => null,
    };
}
