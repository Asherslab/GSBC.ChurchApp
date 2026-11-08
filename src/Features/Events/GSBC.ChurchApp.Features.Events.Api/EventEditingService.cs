using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventEditingService(
    EventsDbContext db, EventsPublisher publisher, EventWriter writer, IAccessEvaluator access, INotifications notifications, TimeProvider time)
    : IEventEditingService
{
    private const int NeedsDetailsDays = 60;
    private const int LongestAgendaDays = 400;
    private const int MostPlacesUsed = 50;

    public async ValueTask<CalendarDesk> DeskAsync(Done request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        ClaimsPrincipal user = UserOf(context);
        CalendarDesk desk = new() { CanManage = await access.IsAllowedAsync(user, EventsPermissions.ManageNode, null, cancellationToken) };
        HashSet<Guid> publishable = [];
        foreach (CalendarRecord calendar in await db.Calendars.AsNoTracking().OrderBy(calendar => calendar.Order).ThenBy(calendar => calendar.Name).ToListAsync(cancellationToken))
        {
            bool canEdit = await CanAsync(user, EventsPermissions.EditNode, calendar.Id, cancellationToken);
            bool canPublish = await CanAsync(user, EventsPermissions.PublishNode, calendar.Id, cancellationToken);
            if (canPublish && calendar.NeedsPublisher)
                publishable.Add(calendar.Id);
            if (canEdit || desk.CanManage)
                desk.Calendars.Add(new DeskCalendar { Settings = CalendarSetupService.Settings(calendar), CanEdit = canEdit, CanPublish = canPublish });
        }

        HashSet<Guid> editable = desk.Calendars.Where(calendar => calendar.CanEdit).Select(calendar => Guid.Parse(calendar.Settings.Calendar.Id)).ToHashSet();
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        desk.NeedsDetails = NeedsDetails(book, editable).Count;
        desk.AskedToPublish = book.Events.Cast<IShowable>().Concat(book.RegularEvents).Count(item => item.AskedAt is not null && publishable.Contains(item.CalendarId));
        desk.AskedToShow = await WaitingToShowAsync(book, editable, cancellationToken);
        return desk;
    }

    public async ValueTask<Agenda> AgendaAsync(AgendaQuery request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        (DateOnly windowFrom, DateOnly windowTo) = EventsBook.Window(time);
        DateOnly from = EventDates.Parse(request.From) ?? windowFrom;
        DateOnly to = EventDates.Parse(request.To) ?? windowTo;
        if (to < from || to.DayNumber - from.DayNumber > LongestAgendaDays)
            throw Invalid($"Ask for at most {LongestAgendaDays} days at a time.");

        HashSet<Guid> editable = await EditableAsync(UserOf(context), cancellationToken);
        HashSet<Guid> wanted = request.CalendarIds.Count == 0
            ? editable
            : request.CalendarIds.Select(id => Guid.TryParse(id, out Guid calendarId) ? calendarId : Guid.Empty).Where(editable.Contains).ToHashSet();
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        return new Agenda
        {
            Occurrences = book.Occurrences(from, to, item => wanted.Contains(item.CalendarId) || book.AlsoOn(item.Id).Any(wanted.Contains))
                .OrderBy(occurrence => occurrence.StartsAt).ThenBy(occurrence => occurrence.Fields.Title).ToList(),
        };
    }

    public async ValueTask<EventDetail> EventAsync(EventRef request, CallContext context = default)
    {
        EventRecord record = await writer.EventInAsync(request.CalendarId, request.EventId, context.CancellationToken);
        return EventWriter.Detail(record, await db.Showings.AsNoTracking().Where(showing => showing.EventId == record.Id).ToListAsync(context.CancellationToken),
            await db.Calendars.AsNoTracking().SingleAsync(calendar => calendar.Id == record.CalendarId, context.CancellationToken));
    }

    public async ValueTask<EventDetail> SaveEventAsync(EventSave request, CallContext context = default) =>
        await writer.SaveEventAsync(UserOf(context), request, context.CancellationToken);

    public async ValueTask<Done> DeleteEventAsync(EventRef request, CallContext context = default)
    {
        await writer.DeleteEventAsync(UserOf(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<RegularEventDetail> RegularEventAsync(RegularRef request, CallContext context = default)
    {
        RegularEventRecord record = await writer.RegularInAsync(request.CalendarId, request.RegularEventId, context.CancellationToken);
        return await writer.DetailAsync(record, context.CancellationToken);
    }

    public async ValueTask<RegularEventSaved> SaveRegularEventAsync(RegularEventSave request, CallContext context = default) =>
        await writer.SaveRegularEventAsync(UserOf(context), request, context.CancellationToken);

    public async ValueTask<Done> DeleteRegularEventAsync(RegularRef request, CallContext context = default)
    {
        await writer.DeleteRegularEventAsync(UserOf(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<RegularEventDetail> SaveDateAsync(DateSave request, CallContext context = default) =>
        await writer.SaveDateAsync(UserOf(context), request, context.CancellationToken);

    public async ValueTask<Done> AskToPublishAsync(ItemRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        (IShowable item, CalendarRecord calendar) = await writer.ItemInAsync(request, cancellationToken);
        if (!calendar.NeedsPublisher)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"{calendar.Name} doesn't need a publisher: show it yourself."));
        if (EventsBook.IsShown(item, time.GetUtcNow().UtcDateTime))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "It's already shown."));

        item.AskedAt = time.GetUtcNow().UtcDateTime;
        item.AskedBy = Caller.UserId(UserOf(context));
        publisher.MarkAdmin(calendar.Id, item.Id);
        await db.SaveChangesAsync(cancellationToken);
        await notifications.RaiseAsync(new Notification(EventsNotifications.AskedToPublish,
            NotificationAudience.Holding(EventsPermissions.Publish, calendar.Id.ToString()),
            $"Waiting to publish: {item.Fields.Title}", $"In {calendar.Name}", Subject: AskedSubject(item.Id), ScopeId: calendar.Id.ToString(),
            Action: "Look"), cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> PublishAsync(ItemRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        (IShowable item, CalendarRecord calendar) = await writer.ItemInAsync(request, cancellationToken);
        if (item.Showing == Showing.SavedForLater || item.ShowFrom is null)
            item.Showing = Showing.Shown;
        item.AskedAt = null;
        item.AskedBy = null;
        item.UpdatedAt = time.GetUtcNow().UtcDateTime;
        publisher.MarkAdmin(calendar.Id, item.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        await notifications.SettleAsync(AskedSubject(item.Id), cancellationToken);
        return new Done();
    }

    public async ValueTask<AskedList> AskedToPublishAsync(Done request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        ClaimsPrincipal user = UserOf(context);
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        AskedList asked = new();
        foreach (IShowable item in book.Events.Cast<IShowable>().Concat(book.RegularEvents).Where(item => item.AskedAt is not null).OrderBy(item => item.AskedAt))
            if (await CanAsync(user, EventsPermissions.PublishNode, item.CalendarId, cancellationToken))
                asked.Items.Add(new AskedItem
                {
                    CalendarId = item.CalendarId.ToString(),
                    EventId = item is EventRecord ? item.Id.ToString() : null,
                    RegularEventId = item is RegularEventRecord ? item.Id.ToString() : null,
                    Title = item.Fields.Title,
                    AskedAt = item.AskedAt!.Value,
                    AskedBy = item.AskedBy?.ToString(),
                });
        return asked;
    }

    public async ValueTask<Done> ShowOnAsync(ShowOnRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        (IShowable item, CalendarRecord home) = await writer.ItemInAsync(new ItemRef { CalendarId = request.CalendarId, EventId = request.EventId, RegularEventId = request.RegularEventId }, cancellationToken);
        if (!Guid.TryParse(request.OtherCalendarId, out Guid otherId) || otherId == home.Id
            || await db.Calendars.AsNoTracking().SingleOrDefaultAsync(calendar => calendar.Id == otherId, cancellationToken) is not { } other)
            throw Invalid("Choose another calendar to show it on.");
        await writer.MayChangeShownAsync(UserOf(context), item, time.GetUtcNow().UtcDateTime, cancellationToken);

        Guid? eventId = item is EventRecord ? item.Id : null;
        Guid? regularId = item is RegularEventRecord ? item.Id : null;
        ShowingRecord? showing = await db.Showings.SingleOrDefaultAsync(
            showing => showing.CalendarId == otherId && showing.EventId == eventId && showing.RegularEventId == regularId, cancellationToken);
        DateTime now = time.GetUtcNow().UtcDateTime;
        ClaimsPrincipal user = UserOf(context);
        bool agreesItself = await CanAsync(user, EventsPermissions.EditNode, otherId, cancellationToken);
        if (!request.Show)
        {
            if (showing is not null)
                db.Showings.Remove(showing);
        }
        else if (showing is null)
        {
            showing = new ShowingRecord
            {
                Id = Guid.CreateVersion7(), CalendarId = otherId, EventId = eventId, RegularEventId = regularId, AskedAt = now, AskedBy = Caller.UserId(user),
                AgreedAt = agreesItself ? now : null, AgreedBy = agreesItself ? Caller.UserId(user) : null,
            };
            db.Showings.Add(showing);
        }

        publisher.MarkAdmin(home.Id, item.Id);
        publisher.MarkAdmin(otherId, item.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        if (request.Show && showing is { AgreedAt: null })
            await notifications.RaiseAsync(new Notification(EventsNotifications.AskedToShow,
                NotificationAudience.Holding(EventsPermissions.Edit, otherId.ToString()),
                $"Show “{item.Fields.Title}” on {other.Name}?", $"{home.Name} asks to show it on your calendar too",
                Subject: ShowingSubject(otherId, item.Id), ScopeId: otherId.ToString(), Action: "Look"), cancellationToken);
        else
            await notifications.SettleAsync(ShowingSubject(otherId, item.Id), cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> AnswerShowingAsync(ShowingAnswer request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid calendarId = Guid.TryParse(request.CalendarId, out Guid id) ? id : throw Invalid("Choose a calendar.");
        Guid? eventId = Guid.TryParse(request.EventId, out Guid parsedEvent) ? parsedEvent : null;
        Guid? regularId = Guid.TryParse(request.RegularEventId, out Guid parsedRegular) ? parsedRegular : null;
        ShowingRecord showing = await db.Showings.SingleOrDefaultAsync(
                showing => showing.CalendarId == calendarId && showing.EventId == eventId && showing.RegularEventId == regularId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Nobody asks to show that here any more."));

        if (request.Agree)
        {
            showing.AgreedAt = time.GetUtcNow().UtcDateTime;
            showing.AgreedBy = Caller.UserId(UserOf(context));
        }
        else
            db.Showings.Remove(showing);
        publisher.MarkAdmin(calendarId, eventId ?? regularId!.Value);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        await notifications.SettleAsync(ShowingSubject(calendarId, eventId ?? regularId!.Value), cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> TellPeopleAsync(TellPeople request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        if (!EventDates.TryReadOccurrenceId(request.OccurrenceId, out Guid itemId, out DateOnly? date))
            throw Invalid("Choose an event to tell people about.");
        (IShowable item, CalendarRecord calendar) = await writer.ItemInAsync(new ItemRef
        {
            CalendarId = request.CalendarId,
            EventId = date is null ? itemId.ToString() : null,
            RegularEventId = date is null ? null : itemId.ToString(),
        }, cancellationToken);
        DateTime now = time.GetUtcNow().UtcDateTime;
        if (!EventsBook.IsShown(item, now))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Show it before telling people about it."));

        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        DateOnly day = date ?? ((EventRecord)item).StartDate;
        EventOccurrence occurrence = book.Occurrences(day, day, candidate => candidate.Id == item.Id).FirstOrDefault(candidate => candidate.Id == request.OccurrenceId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That date isn't on the calendar."));

        string title = request.Title.Trim() is { Length: > 0 } given ? given : occurrence.Fields.Title;
        string body = request.Body.Trim() is { Length: > 0 } words ? words
            : occurrence.NotOn ? "Not on" : occurrence.ChangeNote.Length > 0 ? occurrence.ChangeNote : WhenWords(occurrence);
        await notifications.RaiseOneOfAsync(Tellable(calendar).Select(audience => new Notification(EventsNotifications.Told, audience, title, body,
            Link: EventsDestinations.Event.PathOf(occurrence.Id), Subject: $"events/told/{occurrence.Id}/{now.Ticks}", ScopeId: calendar.Id.ToString(),
            Action: "Open")).ToList(), cancellationToken);
        return new Done();
    }

    public async ValueTask<NeedsDetailsList> NeedsDetailsAsync(Done request, CallContext context = default)
    {
        HashSet<Guid> editable = await EditableAsync(UserOf(context), context.CancellationToken);
        EventsBook book = await EventsBook.LoadAsync(db, context.CancellationToken);
        return new NeedsDetailsList { Items = NeedsDetails(book, editable) };
    }

    public async ValueTask<PlacesUsed> PlacesUsedAsync(Done request, CallContext context = default)
    {
        HashSet<Guid> editable = await EditableAsync(UserOf(context), context.CancellationToken);
        EventsBook book = await EventsBook.LoadAsync(db, context.CancellationToken);
        IEnumerable<(DateTime At, EventPlace? Place)> places = book.Events.Where(record => editable.Contains(record.CalendarId)).Select(record => (record.UpdatedAt, record.Fields.Place))
            .Concat(book.RegularEvents.Where(record => editable.Contains(record.CalendarId)).SelectMany(record =>
                book.SpecialsOf(record.Id).Select(special => (special.UpdatedAt, special.Fields.Place)).Append((record.UpdatedAt, record.Fields.Place))));
        return new PlacesUsed
        {
            Places = places.Where(used => used.Place is { PlaceId: null or "" } place && (place.Name.Length > 0 || place.Address.Length > 0))
                .OrderByDescending(used => used.At)
                .Select(used => used.Place!)
                .DistinctBy(place => (place.Name.Trim().ToLowerInvariant(), place.Address.Trim().ToLowerInvariant()))
                .Take(MostPlacesUsed)
                .ToList(),
        };
    }

    public async ValueTask<BreakList> BreaksAsync(Done request, CallContext context = default) =>
        await CalendarSetupService.BreaksAsync(db, context.CancellationToken);

    public static IEnumerable<NotificationAudience> Tellable(CalendarRecord calendar)
    {
        Platform.Contracts.Configuration.Audience audience = CalendarAudiences.Of(calendar.Audience);
        if (audience.IsForRoles)
            return audience.RoleIds.Select(role => NotificationAudience.Holding(Platform.Permissions.RoleMembership.Of(role)));
        if (audience != Platform.Contracts.Configuration.Audience.Everyone)
            return [NotificationAudience.SignedIn];
        return [calendar.ShownOnNewPhones ? NotificationAudience.Everyone : NotificationAudience.Holding(EventsPermissions.Have, calendar.Id.ToString())];
    }

    private List<NeedsDetailsItem> NeedsDetails(EventsBook book, IReadOnlySet<Guid> editable)
    {
        DateOnly today = ChurchTime.Today(time);
        return book.Occurrences(today, today.AddDays(NeedsDetailsDays), item => editable.Contains(item.CalendarId))
            .Where(occurrence => occurrence is { NotOn: false, Missing.Count: > 0 })
            .OrderBy(occurrence => occurrence.StartsAt)
            .Select(occurrence => new NeedsDetailsItem { Occurrence = occurrence, DaysToGo = EventDates.Parse(occurrence.Date)!.Value.DayNumber - today.DayNumber })
            .ToList();
    }

    private async Task<List<ShowingRequest>> WaitingToShowAsync(EventsBook book, IReadOnlySet<Guid> editable, CancellationToken cancellationToken)
    {
        List<ShowingRecord> waiting = await db.Showings.AsNoTracking().Where(showing => showing.AgreedAt == null).OrderBy(showing => showing.AskedAt).ToListAsync(cancellationToken);
        Dictionary<Guid, IShowable> items = book.Events.Cast<IShowable>().Concat(book.RegularEvents).ToDictionary(item => item.Id);
        return waiting.Where(showing => editable.Contains(showing.CalendarId) && items.ContainsKey(showing.EventId ?? showing.RegularEventId ?? Guid.Empty))
            .Select(showing =>
            {
                IShowable item = items[showing.EventId ?? showing.RegularEventId!.Value];
                Widening widening = CalendarAudiences.Widening(
                    CalendarAudiences.Union(book.AlsoOn(item.Id).Append(item.CalendarId).Where(book.Calendars.ContainsKey)
                        .Select(id => CalendarAudiences.Of(book.Calendars[id].Audience))),
                    book.Calendars.TryGetValue(showing.CalendarId, out CalendarRecord? other) ? CalendarAudiences.Of(other.Audience) : Platform.Contracts.Configuration.Audience.SignedIn);
                return new ShowingRequest
                {
                    CalendarId = showing.CalendarId.ToString(),
                    HomeCalendarId = item.CalendarId.ToString(),
                    EventId = showing.EventId?.ToString(),
                    RegularEventId = showing.RegularEventId?.ToString(),
                    Title = item.Fields.Title,
                    AskedAt = showing.AskedAt,
                    Widens = widening.Widens,
                    NewlyReaches = widening.Words,
                    NewlyReachesRoleIds = widening.NewRoleIds.Select(role => role.ToString()).ToList(),
                };
            })
            .ToList();
    }

    private async Task<HashSet<Guid>> EditableAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        HashSet<Guid> editable = [];
        foreach (Guid id in await db.Calendars.AsNoTracking().Select(calendar => calendar.Id).ToListAsync(cancellationToken))
            if (await CanAsync(user, EventsPermissions.EditNode, id, cancellationToken))
                editable.Add(id);
        return editable;
    }

    private async Task<bool> CanAsync(ClaimsPrincipal user, string node, Guid calendarId, CancellationToken cancellationToken) =>
        await access.IsAllowedAsync(user, node, calendarId.ToString(), cancellationToken);

    private static string WhenWords(EventOccurrence occurrence)
    {
        DateTime start = ChurchTime.ToChurch(occurrence.StartsAt);
        string day = start.ToString("dddd d MMMM", System.Globalization.CultureInfo.InvariantCulture);
        return occurrence.AllDay ? day : $"{day}, {EventDates.Time(start.Hour * 60 + start.Minute)}";
    }

    private static string AskedSubject(Guid itemId) => $"events/asked/{itemId:N}";

    private static string ShowingSubject(Guid calendarId, Guid itemId) => $"events/showing/{calendarId:N}/{itemId:N}";

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static ClaimsPrincipal UserOf(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User ?? new ClaimsPrincipal();
}
