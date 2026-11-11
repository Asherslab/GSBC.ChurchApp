using Grpc.Core;
using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class AnnouncementsService(
    NotificationsDbContext db, NotificationRaiser raiser, PermissionCatalog permissions, DestinationCatalog destinations, IPeopleIndex people, TimeProvider time)
    : IAnnouncementsService
{
    public const int TitleLimit = 80;
    public const int BodyLimit = 600;
    public const string Everyone = "everyone";
    public const string SignedIn = "signed-in";
    public const string Holding = "holding";
    public const string Person = "person";

    public async ValueTask<AnnouncementList> ListAsync(AnnouncementQuery request, CallContext context = default)
    {
        List<AnnouncementRecord> records = await db.Announcements.AsNoTracking()
            .OrderByDescending(announcement => announcement.UpdatedAt).Take(200).ToListAsync(context.CancellationToken);
        List<Guid> sent = records.Where(record => record.NotificationId is not null).Select(record => record.NotificationId!.Value).ToList();
        Dictionary<Guid, int> readBy = await ReadByAsync(sent, context.CancellationToken);
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(
            records.Where(record => record.RecalledById is not null).Select(record => record.RecalledById!.Value).Distinct().ToList(), context.CancellationToken);
        return new AnnouncementList
        {
            Announcements = records.Select(record => Message(record,
                record.State == AnnouncementState.Recalled ? record.ReadByWhenRecalled : record.NotificationId is { } id ? readBy.GetValueOrDefault(id) : 0,
                record.RecalledById is { } recaller ? names.GetValueOrDefault(recaller) : null)).ToList(),
        };
    }

    public async ValueTask<Announcement> SaveAsync(Announcement request, CallContext context = default)
    {
        Guid author = Actor(context);
        if (request.Title.Trim() is not { Length: > 0 and <= TitleLimit } title)
            throw Invalid($"Give it a title of up to {TitleLimit} characters.");
        if (request.Body.Length > BodyLimit)
            throw Invalid($"Keep it to {BodyLimit} characters: a lock screen shows only the start.");
        string? link = string.IsNullOrWhiteSpace(request.Link) ? null : request.Link.Trim();
        if (link is not null && LinkTargets.KindOf(link) is not (LinkKind.InApp or LinkKind.Web))
            throw Invalid("A link is a page in the app (/…) or a web address (https://…).");
        if (link is not null && LinkTargets.KindOf(link) == LinkKind.InApp && destinations.Resolve(link) is null)
            throw Invalid("That isn't a place in the app. Choose one from the list.");
        AudienceOf(request.Audience);

        AnnouncementRecord? record = Guid.TryParse(request.Id, out Guid id) ? await db.Announcements.FindAsync([id], context.CancellationToken) : null;
        if (record is null)
        {
            record = new AnnouncementRecord { Id = Guid.CreateVersion7(), Title = title, AudienceKind = Everyone, AuthorId = author };
            db.Announcements.Add(record);
        }
        else if (record.State is AnnouncementState.Sent or AnnouncementState.Scheduled or AnnouncementState.Recalled)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, record.State == AnnouncementState.Scheduled
                ? "It's scheduled. Cancel it first to change it."
                : "It's been sent, so it can't change. Copy it into a new announcement instead."));
        }

        record.Title = title;
        record.Body = request.Body.Trim();
        record.Link = link;
        record.Urgent = request.Urgent;
        record.AudienceKind = request.Audience!.Kind;
        record.AudienceNode = request.Audience.Node;
        record.AudienceObjectId = string.IsNullOrEmpty(request.Audience.ObjectId) ? null : request.Audience.ObjectId;
        record.AudienceUserId = Guid.TryParse(request.Audience.UserId, out Guid userId) ? userId : null;
        record.State = AnnouncementState.Draft;
        record.UpdatedAt = time.GetUtcNow().UtcDateTime;
        Mark(record);
        await db.SaveChangesAsync(context.CancellationToken);
        return Message(record);
    }

    public async ValueTask<Done> DeleteAsync(AnnouncementRequest request, CallContext context = default)
    {
        AnnouncementRecord record = await FindAsync(request, context.CancellationToken);
        if (record.State is AnnouncementState.Sent or AnnouncementState.Recalled)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "It's been sent, so it stays in the history."));
        db.Announcements.Remove(record);
        Mark(record, deleted: true);
        await db.SaveChangesAsync(context.CancellationToken);
        await raiser.SettleAsync(Subject(record), context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SendToMeAsync(AnnouncementRequest request, CallContext context = default)
    {
        AnnouncementRecord record = await FindAsync(request, context.CancellationToken);
        if (record.State == AnnouncementState.Recalled)
            throw Recalled();
        await raiser.RaiseAsync(Notification(record, NotificationAudience.Person(Actor(context))) with { Test = true }, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Announcement> AskToSendAsync(AnnouncementRequest request, CallContext context = default)
    {
        AnnouncementRecord record = await FindAsync(request, context.CancellationToken);
        if (record.State != AnnouncementState.Draft)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Only an announcement still being written can be put forward."));
        record.State = AnnouncementState.WaitingToSend;
        record.UpdatedAt = time.GetUtcNow().UtcDateTime;
        Mark(record);
        await db.SaveChangesAsync(context.CancellationToken);
        await raiser.RaiseAsync(new Notification(
            AnnouncementNotifications.WaitingToSend,
            NotificationAudience.Holding(NotificationsPermissions.Send),
            $"“{record.Title}” is waiting to be sent",
            "Check it in the admin panel, then send or schedule it.",
            Link: $"/announcements/{record.Id}",
            Subject: Subject(record)) { Actor = Actor(context) }, context.CancellationToken);
        return Message(record);
    }

    public async ValueTask<Reach> ReachAsync(AudienceMessage request, CallContext context = default)
    {
        (int people, int phones) = await raiser.ReachAsync(AudienceOf(request), context.CancellationToken);
        return new Reach { People = people, Phones = phones };
    }

    public async ValueTask<Announcement> SendAsync(AnnouncementRequest request, CallContext context = default)
    {
        AnnouncementRecord record = await FindAsync(request, context.CancellationToken);
        if (record.State == AnnouncementState.Recalled)
            throw Recalled();
        if (record.State is AnnouncementState.Sent or AnnouncementState.Scheduled)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "It's already sent or scheduled."));

        DateTime now = time.GetUtcNow().UtcDateTime;
        if (request.SendAt is { } sendAt && sendAt > now)
        {
            record.State = AnnouncementState.Scheduled;
            record.SendAt = sendAt;
            record.UpdatedAt = now;
            Mark(record);
            await db.SaveChangesAsync(context.CancellationToken);
            await raiser.SettleAsync(Subject(record), context.CancellationToken);
            return Message(record);
        }

        await SendNowAsync(record, context.CancellationToken);
        return Message(record);
    }

    public async ValueTask<Announcement> CancelAsync(AnnouncementRequest request, CallContext context = default)
    {
        AnnouncementRecord record = await FindAsync(request, context.CancellationToken);
        if (record.State != AnnouncementState.Scheduled)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Only a scheduled announcement can be cancelled."));
        record.State = AnnouncementState.Draft;
        record.SendAt = null;
        record.UpdatedAt = time.GetUtcNow().UtcDateTime;
        Mark(record);
        await db.SaveChangesAsync(context.CancellationToken);
        return Message(record);
    }

    public async ValueTask<Announcement> RecallAsync(AnnouncementRequest request, CallContext context = default)
    {
        Guid recaller = Actor(context);
        AnnouncementRecord record = await FindAsync(request, context.CancellationToken);
        if (record.State == AnnouncementState.Recalled)
            return await MessageAsync(record, context.CancellationToken);
        if (record is not { State: AnnouncementState.Sent, NotificationId: { } notificationId })
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Only a sent announcement can be recalled. One that hasn't gone yet can be changed or cancelled."));

        DateTime now = time.GetUtcNow().UtcDateTime;
        record.ReadByWhenRecalled = (await ReadByAsync([notificationId], context.CancellationToken)).GetValueOrDefault(notificationId);
        record.State = AnnouncementState.Recalled;
        record.RecalledAt = now;
        record.RecalledById = recaller;
        record.UpdatedAt = now;
        Mark(record);
        await raiser.RecallAsync(notificationId, context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);
        return await MessageAsync(record, context.CancellationToken);
    }

    public async Task SendNowAsync(AnnouncementRecord record, CancellationToken cancellationToken)
    {
        NotificationAudience audience = AudienceOf(record);
        (int people, int phones) = await raiser.ReachAsync(audience, cancellationToken);
        record.State = AnnouncementState.Sent;
        record.SentAt = time.GetUtcNow().UtcDateTime;
        record.UpdatedAt = record.SentAt.Value;
        record.ReachedPeople = people;
        record.ReachedPhones = phones;
        record.NotificationId = await raiser.RaisedAsync(Notification(record, audience), cancellationToken);
        Mark(record);
        await db.SaveChangesAsync(cancellationToken);
        await raiser.SettleAsync(Subject(record), cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> ReadByAsync(List<Guid> notificationIds, CancellationToken cancellationToken) =>
        await db.Inbox.AsNoTracking()
            .Where(entry => notificationIds.Contains(entry.NotificationId) && entry.ReadAt != null)
            .GroupBy(entry => entry.NotificationId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);

    private async Task<Announcement> MessageAsync(AnnouncementRecord record, CancellationToken cancellationToken) =>
        Message(record, record.ReadByWhenRecalled,
            record.RecalledById is { } recaller ? (await people.NamesAsync([recaller], cancellationToken)).GetValueOrDefault(recaller) : null);

    private static RpcException Recalled() =>
        new(new Status(StatusCode.FailedPrecondition, "It's been recalled. Copy it into a new announcement to send it again."));

    private static string Subject(AnnouncementRecord record) => $"announcement/{record.Id}";

    private void Mark(AnnouncementRecord record, bool deleted = false) =>
        db.FeedChanges.Add(FeedChangeRecord.Marker(NotificationsFeeds.Announcements, record.Id.ToString(), time.GetUtcNow().UtcDateTime, deleted));

    private static Notification Notification(AnnouncementRecord record, NotificationAudience audience) =>
        new(AnnouncementNotifications.Announcement, audience, record.Title, record.Body, Link: record.Link, Urgent: record.Urgent) { Actor = record.AuthorId };

    private NotificationAudience AudienceOf(AnnouncementRecord record) => AudienceOf(new AudienceMessage
    {
        Kind = record.AudienceKind, Node = record.AudienceNode, ObjectId = record.AudienceObjectId, UserId = record.AudienceUserId?.ToString(),
    });

    private NotificationAudience AudienceOf(AudienceMessage? audience) => audience?.Kind switch
    {
        Everyone => NotificationAudience.Everyone,
        SignedIn => NotificationAudience.SignedIn,
        Person when Guid.TryParse(audience.UserId, out Guid userId) => NotificationAudience.Person(userId),
        Holding when audience.Node is { } node && permissions.ByNode.TryGetValue(node, out Permission? permission) =>
            NotificationAudience.Holding(permission, string.IsNullOrEmpty(audience.ObjectId) ? null : audience.ObjectId),
        _ => throw Invalid("Choose who it's for."),
    };

    private async Task<AnnouncementRecord> FindAsync(AnnouncementRequest request, CancellationToken cancellationToken) =>
        (Guid.TryParse(request.Id, out Guid id) ? await db.Announcements.FindAsync([id], cancellationToken) : null)
        ?? throw new RpcException(new Status(StatusCode.NotFound, "That announcement has gone."));

    private static Guid Actor(CallContext context) =>
        Caller.UserId(context.ServerCallContext!.GetHttpContext().User) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static Announcement Message(AnnouncementRecord record) => Message(record, readBy: 0, recalledBy: null);

    private static Announcement Message(AnnouncementRecord record, int readBy, string? recalledBy) => new()
    {
        Id = record.Id.ToString(),
        Title = record.Title,
        Body = record.Body,
        Link = record.Link,
        Audience = new AudienceMessage
        {
            Kind = record.AudienceKind, Node = record.AudienceNode, ObjectId = record.AudienceObjectId, UserId = record.AudienceUserId?.ToString(),
        },
        Urgent = record.Urgent,
        State = record.State.ToString(),
        SendAt = record.SendAt,
        SentAt = record.SentAt,
        AuthorId = record.AuthorId.ToString(),
        UpdatedAt = record.UpdatedAt,
        ReachedPeople = record.ReachedPeople,
        ReachedPhones = record.ReachedPhones,
        ReadBy = readBy,
        RecalledAt = record.RecalledAt,
        RecalledBy = recalledBy,
    };
}
