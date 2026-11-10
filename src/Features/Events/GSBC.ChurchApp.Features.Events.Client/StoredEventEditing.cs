using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed record EditingBook(
    IReadOnlyList<EditingCalendar> Calendars,
    IReadOnlyDictionary<string, EventDetail> Events,
    IReadOnlyDictionary<string, EditingRegular> Regulars)
{
    public static readonly EditingBook Empty = new([], new Dictionary<string, EventDetail>(), new Dictionary<string, EditingRegular>());

    public static EditingBook Of(IEnumerable<StoredRecord> rows)
    {
        List<EditingCalendar> calendars = [];
        Dictionary<string, EventDetail> events = [];
        Dictionary<string, EditingRegular> regulars = [];
        foreach (StoredRecord row in rows)
        {
            MemoryStream payload = new(row.Payload);
            if (row.Id.StartsWith("calendar/", StringComparison.Ordinal))
                calendars.Add(EventsContractsModel.Instance.Deserialize<EditingCalendar>(payload));
            else if (row.Id.StartsWith("event/", StringComparison.Ordinal))
                events[row.Id["event/".Length..]] = EventsContractsModel.Instance.Deserialize<EventDetail>(payload);
            else if (row.Id.StartsWith("regular/", StringComparison.Ordinal))
                regulars[row.Id["regular/".Length..]] = EventsContractsModel.Instance.Deserialize<EditingRegular>(payload);
        }
        return new EditingBook(calendars.OrderBy(calendar => calendar.Settings.Calendar.Order).ThenBy(calendar => calendar.Settings.Calendar.Name).ToList(), events, regulars);
    }

    public EditingCalendar? Calendar(string id) => Calendars.FirstOrDefault(calendar => calendar.Settings.Calendar.Id == id);
}

public sealed class StoredEventEditing(ILocalFeeds feeds, QueuedEventEdits queue, IAccess access, CalendarDiary diary, TimeProvider clock) : IEventEditing
{
    private const int Page = 500;
    private const string NeedsTheApp = "Saving needs the app. Try again on your phone.";

    public IReadOnlyList<string> Feeds { get; } = [EventsFeeds.Editing, .. CalendarDiary.Feeds];

    public event Action? WaitingChanged
    {
        add => queue.Changed += value;
        remove => queue.Changed -= value;
    }

    private DateOnly Today => ChurchTime.DayOf(clock.GetUtcNow().UtcDateTime);

    public async Task<CalendarDesk> DeskAsync()
    {
        EditingBook book = await ReadAsync();
        CalendarDesk desk = new() { CanManage = access.Can(EventsPermissions.Manage) };
        desk.Calendars.AddRange(book.Calendars.Where(calendar => access.Can(EventsPermissions.Edit, calendar.Settings.Calendar.Id)).Select(calendar => new DeskCalendar
        {
            Settings = calendar.Settings, CanEdit = true, CanPublish = access.Can(EventsPermissions.Publish, calendar.Settings.Calendar.Id),
        }));
        if (desk.CanManage)
            desk.Calendars.AddRange((await diary.LoadAsync()).Calendars.Where(calendar => desk.Calendars.All(known => known.Settings.Calendar.Id != calendar.Id))
                .Select(calendar => new DeskCalendar { Settings = new CalendarSettings { Calendar = calendar } }));
        return desk;
    }

    public async Task<EventDetail?> EventAsync(string calendarId, string eventId)
    {
        EditingBook book = await ReadAsync();
        EventDetail? detail = book.Events.GetValueOrDefault(eventId);
        foreach (QueuedEdit edit in await QueuedForAsync(EditKeys.Event(eventId)))
            detail = edit.Method switch
            {
                QueuedEventEdits.SaveEvent => EditSaves.Applied(detail, QueuedEventEdits.Read<EventSave>(edit)),
                QueuedEventEdits.DeleteEvent => null,
                _ => detail,
            };
        return detail;
    }

    public async Task<RegularEventDetail?> RegularEventAsync(string calendarId, string regularEventId) =>
        Waiting(await ReadAsync(), regularEventId, await QueuedForAsync(EditKeys.Regular(regularEventId)));

    public async Task<IReadOnlyList<RegularEventSummary>> RegularEventsAsync(string calendarId)
    {
        EditingBook book = await ReadAsync();
        IReadOnlyList<QueuedEdit> queued = await queue.WaitingAsync();
        IEnumerable<string> ids = book.Regulars.Keys
            .Concat(queued.Where(edit => edit.Method == QueuedEventEdits.SaveRegularEvent).Select(edit => QueuedEventEdits.Read<RegularEventSave>(edit)).Select(save => save.RegularEventId.Length > 0 ? save.RegularEventId : save.NewId))
            .Distinct();
        return ids.Select(id => (Id: id, Stored: book.Regulars.GetValueOrDefault(id), Detail: Waiting(book, id, queued.Where(edit => QueuedEventEdits.Concerns(edit.Key, EditKeys.Regular(id))).ToList())))
            .Where(entry => entry.Detail?.CalendarId == calendarId)
            .Select(entry => Summary(entry.Stored?.Summary, entry.Detail!))
            .OrderBy(summary => summary.Ended).ThenBy(summary => summary.NextDate.Length == 0).ThenBy(summary => summary.NextDate, StringComparer.Ordinal)
            .ThenBy(summary => summary.StartMinute ?? -1).ThenBy(summary => summary.Title, StringComparer.CurrentCulture)
            .ToList();
    }

    public async Task<IReadOnlyList<EventPlace>> PlacesUsedAsync() => EditingOffers.PlacesUsed(await ReadAsync());

    public async Task<IReadOnlyList<MediaRef>?> PicturesUsedAsync(string calendarId) => EditingOffers.PicturesUsed(await ReadAsync(), calendarId);

    public async Task<BreakList> BreaksAsync() => new() { Breaks = (await ReadAsync()).Calendars.FirstOrDefault()?.Breaks ?? [] };

    public async Task<ShowableList> ShowableAsync(string calendarId, string search) =>
        EditingOffers.Showable(await diary.LoadAsync(), await ReadAsync(), calendarId, search, Today, id => access.Can(EventsPermissions.Edit, id));

    public Task<EditSent> SaveEventAsync(EventSave save) =>
        QueueAsync(EditKeys.Event(save.EventId.Length > 0 ? save.EventId : save.NewId), QueuedEventEdits.SaveEvent, save);

    public Task<EditSent> SaveRegularEventAsync(RegularEventSave save) =>
        QueueAsync(EditKeys.Regular(save.RegularEventId.Length > 0 ? save.RegularEventId : save.NewId), QueuedEventEdits.SaveRegularEvent, save);

    public Task<EditSent> SaveDateAsync(DateSave save) => QueueAsync(EditKeys.Date(save.RegularEventId, save.Date), QueuedEventEdits.SaveDate, save);

    public Task<EditSent> DeleteEventAsync(EventRef target) => QueueAsync(EditKeys.Event(target.EventId), QueuedEventEdits.DeleteEvent, target);

    public Task<EditSent> DeleteRegularEventAsync(RegularRef target) =>
        QueueAsync(EditKeys.Regular(target.RegularEventId), QueuedEventEdits.DeleteRegularEvent, target);

    public Task<EditSent> AskToPublishAsync(ItemRef item) => QueueAsync(KeyOf(item.EventId, item.RegularEventId), QueuedEventEdits.AskToPublish, item);

    public Task<EditSent> ShowOnAsync(ShowOnRequest request) => QueueAsync(KeyOf(request.EventId, request.RegularEventId), QueuedEventEdits.ShowOn, request);

    public Task<EditSent> ShowHereAsync(ShowHereRequest request) => QueueAsync(KeyOf(request.EventId, request.RegularEventId), QueuedEventEdits.ShowHere, request);

    public Task<EditSent> TellPeopleAsync(TellPeople request) =>
        QueueAsync(EditKeys.OfOccurrence(request.OccurrenceId) ?? request.OccurrenceId, QueuedEventEdits.TellPeople, request);

    public EditWaiting? WaitingFor(string itemKey) => queue.WaitingFor(itemKey);

    public Task RetryAsync(string itemKey) => queue.RetryAsync(itemKey);

    public string Explain(Exception exception) => "Something went wrong. Try again in a moment.";

    private async Task<EditSent> QueueAsync<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] T>(string key, string method, T request)
    {
        if (!queue.CanQueue)
            return new EditSent(NeedsTheApp);
        await queue.QueueAsync(key, method, request);
        return EditSent.Waiting;
    }

    private static string KeyOf(string? eventId, string? regularEventId) =>
        regularEventId is { Length: > 0 } regular ? EditKeys.Regular(regular) : EditKeys.Event(eventId ?? "");

    private async Task<IReadOnlyList<QueuedEdit>> QueuedForAsync(string key) =>
        (await queue.WaitingAsync()).Where(edit => QueuedEventEdits.Concerns(edit.Key, key)).ToList();

    private static RegularEventDetail? Waiting(EditingBook book, string regularEventId, IReadOnlyList<QueuedEdit> queued)
    {
        RegularEventDetail? detail = book.Regulars.GetValueOrDefault(regularEventId)?.Detail;
        foreach (QueuedEdit edit in queued)
            detail = edit.Method switch
            {
                QueuedEventEdits.SaveRegularEvent => EditSaves.Applied(detail, QueuedEventEdits.Read<RegularEventSave>(edit)),
                QueuedEventEdits.SaveDate when detail is not null => EditSaves.Applied(detail, QueuedEventEdits.Read<DateSave>(edit)),
                QueuedEventEdits.DeleteRegularEvent => null,
                _ => detail,
            };
        return detail;
    }

    private static RegularEventSummary Summary(RegularEventSummary? stored, RegularEventDetail detail) => new()
    {
        Id = detail.Id,
        CalendarId = detail.CalendarId,
        Title = detail.Fields.Title,
        Repeats = detail.Repeats.Length > 0 ? detail.Repeats : RepeatWords.Of(detail.Pattern),
        StartMinute = detail.StartMinute,
        EndMinute = detail.EndMinute,
        NextDate = stored?.NextDate ?? "",
        ChangedDates = detail.Dates.Count(date => date.Changed),
        Showing = detail.Showing,
        Asked = detail.Asked,
        Ended = stored?.Ended ?? false,
    };

    private async Task<EditingBook> ReadAsync()
    {
        if (!feeds.IsAvailable || !access.SignedIn)
            return EditingBook.Empty;
        List<StoredRecord> rows = [];
        for (int offset = 0; ; offset += Page)
        {
            IReadOnlyList<StoredRecord> page = await feeds.ReadPageAsync(new FeedPageQuery(EventsFeeds.Editing, Limit: Page, Offset: offset));
            rows.AddRange(page);
            if (page.Count < Page)
                return EditingBook.Of(rows);
        }
    }
}
