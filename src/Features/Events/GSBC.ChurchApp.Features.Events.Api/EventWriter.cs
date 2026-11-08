using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventWriter(EventsDbContext db, EventsPublisher publisher, IAccessEvaluator access, IMediaReferences media, TimeProvider time)
{
    public const int LongestTitle = 120;
    private const int LongestLabel = 40;
    public const int MostRemindDays = 60;
    private static readonly IReadOnlyList<int> DefaultRemindDays = [7, 2];

    public async Task<EventRecord> EventInAsync(string calendarId, string eventId, CancellationToken cancellationToken) =>
        Guid.TryParse(calendarId, out Guid calendar) && Guid.TryParse(eventId, out Guid id)
        && await db.Events.SingleOrDefaultAsync(record => record.Id == id, cancellationToken) is { } found && found.CalendarId == calendar
            ? found
            : throw NotHere("That event isn't in this calendar.");

    public async Task<RegularEventRecord> RegularInAsync(string calendarId, string regularEventId, CancellationToken cancellationToken) =>
        Guid.TryParse(calendarId, out Guid calendar) && Guid.TryParse(regularEventId, out Guid id)
        && await db.RegularEvents.SingleOrDefaultAsync(record => record.Id == id, cancellationToken) is { } found && found.CalendarId == calendar
            ? found
            : throw NotHere("That regular event isn't in this calendar.");

    public async Task<(IShowable Item, CalendarRecord Calendar)> ItemInAsync(ItemRef request, CancellationToken cancellationToken)
    {
        IShowable item = request.RegularEventId is { Length: > 0 } regularId
            ? await RegularInAsync(request.CalendarId, regularId, cancellationToken)
            : await EventInAsync(request.CalendarId, request.EventId ?? "", cancellationToken);
        return (item, await db.Calendars.SingleAsync(calendar => calendar.Id == item.CalendarId, cancellationToken));
    }

    public async Task<EventDetail> SaveEventAsync(ClaimsPrincipal user, EventSave request, CancellationToken cancellationToken)
    {
        CalendarRecord calendar = await WritableCalendarAsync(request.CalendarId, cancellationToken);
        EventRecord? record = request.EventId.Length > 0 ? await db.Events.SingleOrDefaultAsync(found => found.Id == ParseId(request.EventId), cancellationToken) ?? throw NotHere("That event isn't here any more.") : null;
        DateTime now = time.GetUtcNow().UtcDateTime;
        if (record is not null && record.CalendarId != calendar.Id)
        {
            if (!await CanAsync(user, EventsPermissions.EditNode, record.CalendarId, cancellationToken))
                throw Refused("You can't move events out of that calendar.");
            await WritableCalendarAsync(record.CalendarId.ToString(), cancellationToken);
        }

        EventFields fields = await CheckedAsync(request.Fields, cancellationToken);
        (DateOnly startDate, DateOnly? endDate, int? startMinute, int? endMinute) = Times(request.Times);
        DateTime? showFrom = ShowFromOf(request.Showing, request.ShowFrom);
        await MayShowAsync(user, calendar, record, request.Showing, now, cancellationToken);
        if (record is not null)
            await MayChangeShownAsync(user, record, now, cancellationToken);

        bool isNew = record is null;
        if (record is null)
        {
            record = new EventRecord { Id = Guid.CreateVersion7(), CalendarId = calendar.Id, CreatedAt = now };
            db.Events.Add(record);
        }
        else if (EventsBook.IsShown(record, now) && Shown(request.Showing, request.ShowFrom, now))
        {
            string note = record.NotOn != request.NotOn
                ? request.NotOn ? "Not on" : "Back on"
                : FieldValues.ChangeNote(new Timed(record.Fields, record.StartMinute, record.EndMinute), new Timed(fields, startMinute, endMinute), record.StartDate, startDate);
            if (note.Length > 0)
                (record.ChangeNote, record.ChangedAt) = (note, now);
        }

        Guid before = record.CalendarId;
        record.CalendarId = calendar.Id;
        record.Fields = fields;
        (record.StartDate, record.EndDate, record.StartMinute, record.EndMinute) = (startDate, endDate, startMinute, endMinute);
        (record.Showing, record.ShowFrom) = (request.Showing, showFrom);
        (record.NotOn, record.NotOnNote) = (request.NotOn, request.NotOn ? request.NotOnNote.Trim() : "");
        if (IsOrWillBeShown(record, now))
            (record.AskedAt, record.AskedBy) = (null, null);
        record.UpdatedAt = now;

        await UsePicturesAsync(EventOwner(record.Id), fields, cancellationToken);
        publisher.MarkAdmin(calendar.Id, record.Id);
        if (!isNew && before != calendar.Id)
            publisher.MarkAdmin(before, record.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        return Detail(record, await db.Showings.AsNoTracking().Where(showing => showing.EventId == record.Id).ToListAsync(cancellationToken), calendar);
    }

    public async Task DeleteEventAsync(ClaimsPrincipal user, EventRef request, CancellationToken cancellationToken)
    {
        EventRecord record = await EventInAsync(request.CalendarId, request.EventId, cancellationToken);
        await WritableCalendarAsync(request.CalendarId, cancellationToken);
        await MayChangeShownAsync(user, record, time.GetUtcNow().UtcDateTime, cancellationToken);
        db.Showings.RemoveRange(db.Showings.Where(showing => showing.EventId == record.Id));
        db.Events.Remove(record);
        publisher.MarkAdmin(record.CalendarId, record.Id);
        await media.UseAsync(EventOwner(record.Id), [], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
    }

    public async Task<RegularEventSaved> SaveRegularEventAsync(ClaimsPrincipal user, RegularEventSave request, CancellationToken cancellationToken)
    {
        CalendarRecord calendar = await WritableCalendarAsync(request.CalendarId, cancellationToken);
        RegularEventRecord? record = request.RegularEventId.Length > 0
            ? await db.RegularEvents.SingleOrDefaultAsync(found => found.Id == ParseId(request.RegularEventId), cancellationToken) ?? throw NotHere("That regular event isn't here any more.")
            : null;
        DateTime now = time.GetUtcNow().UtcDateTime;
        if (record is not null && record.CalendarId != calendar.Id)
        {
            if (!await CanAsync(user, EventsPermissions.EditNode, record.CalendarId, cancellationToken))
                throw Refused("You can't move regular events out of that calendar.");
            await WritableCalendarAsync(record.CalendarId.ToString(), cancellationToken);
        }

        EventFields fields = await CheckedAsync(request.Fields, cancellationToken);
        RegularPattern pattern = Checked(request.Pattern, ChurchTime.Today(time));
        (int? startMinute, int? endMinute) = Minutes(request.StartMinute, request.EndMinute);
        List<string> needs = request.Needs.Where(EventFieldKeys.Needable.Contains).Distinct().ToList();
        List<int> remind = request.RemindDaysBefore.Where(days => days is >= 1 and <= MostRemindDays).Distinct().OrderDescending().ToList();
        if (needs.Count > 0 && remind.Count == 0)
            remind = [.. DefaultRemindDays];
        List<Guid> breakIds = request.BreakIds.Select(ParseId).Distinct().ToList();
        if (await db.Breaks.CountAsync(stop => breakIds.Contains(stop.Id) && stop.RegularEventId == null, cancellationToken) != breakIds.Count)
            throw Invalid("One of those breaks isn't here any more.");
        List<(Guid? Id, string Name, DateOnly Start, DateOnly End)> ownBreaks = request.OwnBreaks.Select(CheckedBreak).ToList();

        DateTime? showFrom = ShowFromOf(request.Showing, request.ShowFrom);
        await MayShowAsync(user, calendar, record, request.Showing, now, cancellationToken);
        if (record is not null)
            await MayChangeShownAsync(user, record, now, cancellationToken);

        Timed after = new(fields, startMinute, endMinute);
        if (record is not null)
        {
            Timed before = new(record.Fields, record.StartMinute, record.EndMinute);
            List<string> changed = FieldValues.Differences(before, after);
            List<KeptValue> kept = await KeptValuesAsync(record, changed, cancellationToken);
            if (kept.Count > 0 && !request.KeepOwnConfirmed)
                return new RegularEventSaved { Saved = false, KeepOwn = kept };
            if (EventsBook.IsShown(record, now) && Shown(request.Showing, request.ShowFrom, now) && FieldValues.ChangeNote(before, after) is { Length: > 0 } note)
                (record.ChangeNote, record.ChangedAt) = (note, now);
        }

        bool isNew = record is null;
        if (record is null)
        {
            record = new RegularEventRecord { Id = Guid.CreateVersion7(), CalendarId = calendar.Id, CreatedAt = now };
            db.RegularEvents.Add(record);
        }
        Guid previousCalendar = record.CalendarId;
        record.CalendarId = calendar.Id;
        record.Fields = fields;
        record.Pattern = pattern;
        (record.StartMinute, record.EndMinute) = (startMinute, endMinute);
        (record.Showing, record.ShowFrom) = (request.Showing, showFrom);
        if (IsOrWillBeShown(record, now))
            (record.AskedAt, record.AskedBy) = (null, null);
        record.Needs = needs;
        record.RemindDaysBefore = remind;
        record.ListEveryDate = request.ListEveryDate;
        record.UpdatedAt = now;

        db.RegularEventBreaks.RemoveRange(db.RegularEventBreaks.Where(link => link.RegularEventId == record.Id));
        db.RegularEventBreaks.AddRange(breakIds.Select(breakId => new RegularEventBreak { RegularEventId = record.Id, BreakId = breakId }));
        List<BreakRecord> existingOwn = await db.Breaks.Where(stop => stop.RegularEventId == record.Id).ToListAsync(cancellationToken);
        db.Breaks.RemoveRange(existingOwn.Where(stop => ownBreaks.All(own => own.Id != stop.Id)));
        foreach ((Guid? id, string name, DateOnly start, DateOnly end) in ownBreaks)
        {
            BreakRecord stop = existingOwn.FirstOrDefault(found => found.Id == id) ?? db.Breaks.Add(new BreakRecord
            {
                Id = Guid.CreateVersion7(), Name = name, RegularEventId = record.Id, CreatedAt = now,
            }).Entity;
            (stop.Name, stop.StartDate, stop.EndDate) = (name, start, end);
        }

        await UsePicturesAsync(RegularOwner(record.Id), fields, cancellationToken);
        publisher.MarkAdmin(calendar.Id, record.Id);
        if (!isNew && previousCalendar != calendar.Id)
            publisher.MarkAdmin(previousCalendar, record.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        return new RegularEventSaved { Saved = true, Detail = await DetailAsync(record, cancellationToken) };
    }

    public async Task DeleteRegularEventAsync(ClaimsPrincipal user, RegularRef request, CancellationToken cancellationToken)
    {
        RegularEventRecord record = await RegularInAsync(request.CalendarId, request.RegularEventId, cancellationToken);
        await WritableCalendarAsync(request.CalendarId, cancellationToken);
        await MayChangeShownAsync(user, record, time.GetUtcNow().UtcDateTime, cancellationToken);
        List<DateOnly> specialDates = await db.Specials.Where(special => special.RegularEventId == record.Id).Select(special => special.Date).ToListAsync(cancellationToken);
        db.Specials.RemoveRange(db.Specials.Where(special => special.RegularEventId == record.Id));
        db.Breaks.RemoveRange(db.Breaks.Where(stop => stop.RegularEventId == record.Id));
        db.RegularEventBreaks.RemoveRange(db.RegularEventBreaks.Where(link => link.RegularEventId == record.Id));
        db.Showings.RemoveRange(db.Showings.Where(showing => showing.RegularEventId == record.Id));
        db.RegularEvents.Remove(record);
        publisher.MarkAdmin(record.CalendarId, record.Id);
        await media.UseAsync(RegularOwner(record.Id), [], cancellationToken);
        foreach (DateOnly date in specialDates)
            await media.UseAsync(SpecialOwner(record.Id, date), [], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
    }

    public async Task<RegularEventDetail> SaveDateAsync(ClaimsPrincipal user, DateSave request, CancellationToken cancellationToken)
    {
        RegularEventRecord regular = await RegularInAsync(request.CalendarId, request.RegularEventId, cancellationToken);
        await WritableCalendarAsync(request.CalendarId, cancellationToken);
        DateTime now = time.GetUtcNow().UtcDateTime;
        await MayChangeShownAsync(user, regular, now, cancellationToken);
        DateOnly date = EventDates.Parse(request.Date) ?? throw Invalid("Choose a date.");
        if (!Expansion.DatesOf(regular.Pattern, date, date).Contains(date))
            throw Invalid($"{date.ToString("d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture)} isn't one of {regular.Fields.Title}'s dates.");

        EventFields fields = await CheckedAsync(request.Fields, cancellationToken);
        (int? startMinute, int? endMinute) = Minutes(request.StartMinute, request.EndMinute);
        Timed every = new(regular.Fields, regular.StartMinute, regular.EndMinute);
        Timed thisDate = new(fields, startMinute, endMinute);
        List<string> changed = FieldValues.Differences(every, thisDate);

        SpecialRecord? special = await db.Specials.SingleOrDefaultAsync(found => found.RegularEventId == regular.Id && found.Date == date, cancellationToken);
        Timed before = special is null ? every : Effective(regular, special);
        bool wasNotOn = special?.NotOn == true;
        if (changed.Count == 0 && !request.NotOn)
        {
            if (special is not null)
                db.Specials.Remove(special);
        }
        else
        {
            if (special is null)
            {
                special = new SpecialRecord { RegularEventId = regular.Id, Date = date };
                db.Specials.Add(special);
            }
            special.Changed = changed;
            special.Fields = fields;
            (special.StartMinute, special.EndMinute) = (startMinute, endMinute);
            (special.NotOn, special.NotOnNote) = (request.NotOn, request.NotOn ? request.NotOnNote.Trim() : "");
            special.UpdatedAt = now;
            if (EventsBook.IsShown(regular, now))
            {
                string note = wasNotOn != request.NotOn
                    ? request.NotOn ? "Not on" : "Back on"
                    : FieldValues.ChangeNote(before, thisDate);
                if (note.Length > 0)
                    (special.ChangeNote, special.ChangedAt) = (note, now);
            }
        }

        await UsePicturesAsync(SpecialOwner(regular.Id, date), changed.Count == 0 ? new EventFields() : FieldValues.Overlay(new EventFields(), fields, changed), cancellationToken);
        publisher.MarkAdmin(regular.CalendarId, regular.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        return await DetailAsync(regular, cancellationToken);
    }

    public async Task<RegularEventDetail> DetailAsync(RegularEventRecord record, CancellationToken cancellationToken)
    {
        List<SpecialRecord> specials = await db.Specials.AsNoTracking().Where(special => special.RegularEventId == record.Id).OrderBy(special => special.Date).ToListAsync(cancellationToken);
        List<Guid> named = await db.RegularEventBreaks.AsNoTracking().Where(link => link.RegularEventId == record.Id).Select(link => link.BreakId).ToListAsync(cancellationToken);
        List<BreakRecord> own = await db.Breaks.AsNoTracking().Where(stop => stop.RegularEventId == record.Id).OrderBy(stop => stop.StartDate).ToListAsync(cancellationToken);
        List<BreakRecord> namedBreaks = await db.Breaks.AsNoTracking().Where(stop => named.Contains(stop.Id)).ToListAsync(cancellationToken);
        List<ShowingRecord> showings = await db.Showings.AsNoTracking().Where(showing => showing.RegularEventId == record.Id).ToListAsync(cancellationToken);
        DateOnly today = ChurchTime.Today(time);
        return new RegularEventDetail
        {
            Id = record.Id.ToString(),
            CalendarId = record.CalendarId.ToString(),
            Fields = record.Fields,
            Pattern = record.Pattern,
            StartMinute = record.StartMinute,
            EndMinute = record.EndMinute,
            Showing = record.Showing,
            ShowFrom = record.ShowFrom,
            Asked = record.AskedAt is not null,
            Needs = record.Needs,
            RemindDaysBefore = record.RemindDaysBefore,
            ListEveryDate = record.ListEveryDate,
            BreakIds = named.Select(id => id.ToString()).ToList(),
            OwnBreaks = own.Select(stop => CalendarSetupService.Info(stop, [])).ToList(),
            Specials = specials.Select(Info).ToList(),
            Repeats = RepeatWords.Of(record.Pattern),
            AlsoOn = showings.Select(showing => new ShowingOn { CalendarId = showing.CalendarId.ToString(), Agreed = showing.AgreedAt is not null, AskedByOther = showing.AskedByOther }).ToList(),
            ChangeNote = record.ChangeNote,
            ChangedAt = record.ChangedAt,
            UpdatedAt = record.UpdatedAt,
            Dates = Expansion.RunOf(record, specials.ToDictionary(special => special.Date), [.. namedBreaks, .. own], today, today.AddMonths(EventsFeeds.MonthsAhead)).ToList(),
        };
    }

    public static EventDetail Detail(EventRecord record, IReadOnlyList<ShowingRecord> showings, CalendarRecord calendar) => new()
    {
        Id = record.Id.ToString(),
        CalendarId = record.CalendarId.ToString(),
        Fields = record.Fields,
        Times = new EventTimes
        {
            StartDate = EventDates.Format(record.StartDate),
            EndDate = record.EndDate is { } end ? EventDates.Format(end) : "",
            StartMinute = record.StartMinute,
            EndMinute = record.EndMinute,
        },
        Showing = record.Showing,
        ShowFrom = record.ShowFrom,
        Asked = record.AskedAt is not null,
        AlsoOn = showings.Select(showing => new ShowingOn { CalendarId = showing.CalendarId.ToString(), Agreed = showing.AgreedAt is not null, AskedByOther = showing.AskedByOther }).ToList(),
        ChangeNote = record.ChangeNote,
        ChangedAt = record.ChangedAt,
        NotOn = record.NotOn,
        NotOnNote = record.NotOnNote,
        UpdatedAt = record.UpdatedAt,
        ReadOnly = calendar.ICalSource is { Length: > 0 },
    };

    internal static string EventOwner(Guid id) => $"events/event/{id}";

    internal static string RegularOwner(Guid id) => $"events/regular/{id}";

    internal static string SpecialOwner(Guid regularEventId, DateOnly date) => $"events/regular/{regularEventId}/{EventDates.Format(date)}";

    private static Timed Effective(RegularEventRecord regular, SpecialRecord special)
    {
        bool ownTime = special.Changed.Contains(EventFieldKeys.Time);
        return new Timed(FieldValues.Overlay(regular.Fields, special.Fields, special.Changed),
            ownTime ? special.StartMinute : regular.StartMinute, ownTime ? special.EndMinute : regular.EndMinute);
    }

    private static SpecialInfo Info(SpecialRecord special) => new()
    {
        Date = EventDates.Format(special.Date),
        Changed = special.Changed,
        Fields = special.Fields,
        StartMinute = special.StartMinute,
        EndMinute = special.EndMinute,
        NotOn = special.NotOn,
        NotOnNote = special.NotOnNote,
        ChangeNote = special.ChangeNote,
        ChangedAt = special.ChangedAt,
    };

    public static List<KeptValue> KeptValues(RegularEventRecord record, IReadOnlyCollection<string> changed, IEnumerable<SpecialRecord> upcoming) =>
        upcoming.SelectMany(special => changed.Where(special.Changed.Contains).Select(field => new KeptValue
        {
            Date = EventDates.Format(special.Date),
            Title = Effective(record, special).Fields.Title,
            Field = field,
            Value = FieldValues.Words(Effective(record, special), field),
        })).ToList();

    private async Task<List<KeptValue>> KeptValuesAsync(RegularEventRecord record, List<string> changed, CancellationToken cancellationToken)
    {
        if (changed.Count == 0)
            return [];
        DateOnly today = ChurchTime.Today(time);
        List<SpecialRecord> upcoming = await db.Specials.AsNoTracking()
            .Where(special => special.RegularEventId == record.Id && special.Date >= today).OrderBy(special => special.Date).ToListAsync(cancellationToken);
        return KeptValues(record, changed, upcoming);
    }

    private async Task<CalendarRecord> WritableCalendarAsync(string calendarId, CancellationToken cancellationToken)
    {
        CalendarRecord calendar = Guid.TryParse(calendarId, out Guid id) && await db.Calendars.SingleOrDefaultAsync(calendar => calendar.Id == id, cancellationToken) is { } found
            ? found
            : throw NotHere("That calendar isn't here any more.");
        if (calendar.ICalSource is { Length: > 0 })
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"{calendar.Name} is copied from another calendar: change it there."));
        return calendar;
    }

    private async Task MayShowAsync(ClaimsPrincipal user, CalendarRecord calendar, IShowable? record, Showing showing, DateTime now, CancellationToken cancellationToken)
    {
        if (ShowingNeedsAPublisher(calendar, record, showing, now) && !await CanAsync(user, EventsPermissions.PublishNode, calendar.Id, cancellationToken))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"{calendar.Name} needs a publisher: save it for later and ask to publish."));
    }

    public async Task MayChangeShownAsync(ClaimsPrincipal user, IShowable record, DateTime now, CancellationToken cancellationToken)
    {
        if (!IsOrWillBeShown(record, now))
            return;
        CalendarRecord calendar = await db.Calendars.AsNoTracking().SingleAsync(found => found.Id == record.CalendarId, cancellationToken);
        if (ChangeNeedsAPublisher(calendar, record, now) && !await CanAsync(user, EventsPermissions.PublishNode, calendar.Id, cancellationToken))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"{calendar.Name} needs a publisher: a publisher changes events once they're shown."));
    }

    public static bool ShowingNeedsAPublisher(CalendarRecord calendar, IShowable? record, Showing showing, DateTime now) =>
        calendar.NeedsPublisher && showing != Showing.SavedForLater && !(record is not null && IsOrWillBeShown(record, now) && record.CalendarId == calendar.Id);

    public static bool ChangeNeedsAPublisher(CalendarRecord calendar, IShowable record, DateTime now) =>
        calendar.NeedsPublisher && IsOrWillBeShown(record, now);

    private static bool IsOrWillBeShown(IShowable record, DateTime now) => EventsBook.IsShown(record, now) || record.Showing == Showing.ShownFrom;

    private async Task<bool> CanAsync(ClaimsPrincipal user, string node, Guid calendarId, CancellationToken cancellationToken) =>
        await access.IsAllowedAsync(user, node, calendarId.ToString(), cancellationToken);

    private async Task<EventFields> CheckedAsync(EventFields fields, CancellationToken cancellationToken)
    {
        string title = fields.Title.Trim();
        if (title.Length is 0 or > LongestTitle)
            throw Invalid($"Give it a title of up to {LongestTitle} characters.");
        if (fields.Label.Trim().Length > LongestLabel)
            throw Invalid($"Keep the label to {LongestLabel} characters.");
        if (fields.Description is { Markdown.Length: > RichText.MaximumLength })
            throw Invalid("The description is too long.");
        if (fields.RegisterLink.Trim() is { Length: > 0 } register && !IsWebAddress(register))
            throw Invalid("The Register link needs to be a web address.");
        if (fields.Links.Any(link => !IsWebAddress(link.Url.Trim())))
            throw Invalid("Each link needs to be a web address.");

        List<MediaRef> claimed = Pictures(fields);
        IReadOnlyList<MediaRef> verified = claimed.Count == 0 ? [] : await media.VerifyAsync(claimed, cancellationToken);
        MediaRef? picture = fields.Picture is null ? null : verified[0];
        RichText? checkedDescription = fields.Description is { IsEmpty: false } text
            ? new RichText { Markdown = text.Markdown.Trim(), Media = verified.Skip(picture is null ? 0 : 1).ToList() }
            : null;

        return new EventFields
        {
            Title = title,
            Label = fields.Label.Trim(),
            Picture = picture,
            Description = checkedDescription,
            Place = fields.Place is { } place && (place.PlaceId is { Length: > 0 } || place.Name.Trim().Length > 0 || place.Address.Trim().Length > 0)
                ? new EventPlace { PlaceId = place.PlaceId is { Length: > 0 } ? place.PlaceId : null, Name = place.Name.Trim(), Address = place.Address.Trim() }
                : null,
            Contacts = fields.Contacts.Where(contact => contact.Name.Trim().Length > 0).Select(contact => new EventContact
            {
                PersonId = contact.PersonId is { Length: > 0 } ? contact.PersonId : null,
                Name = contact.Name.Trim(),
                Phone = contact.Phone.Trim(),
                Email = contact.Email.Trim(),
            }).ToList(),
            RegisterLink = fields.RegisterLink.Trim(),
            Links = fields.Links.Select(link => new EventLink { Title = link.Title.Trim(), Url = link.Url.Trim() }).ToList(),
        };
    }

    private async Task UsePicturesAsync(string owner, EventFields fields, CancellationToken cancellationToken) =>
        await media.UseAsync(owner, Pictures(fields), cancellationToken);

    private static List<MediaRef> Pictures(EventFields fields)
    {
        List<MediaRef> pictures = [.. fields.Description?.Media ?? []];
        if (fields.Picture is { } picture)
            pictures.Insert(0, picture);
        return pictures;
    }

    private static RegularPattern Checked(RegularPattern pattern, DateOnly today)
    {
        DateOnly start = EventDates.Parse(pattern.StartDate) ?? today;
        DateOnly? end = EventDates.Parse(pattern.EndDate);
        if (end < start)
            throw Invalid("It can't end before it starts.");
        List<DayOfWeek> days = pattern.Days.Where(Enum.IsDefined).Distinct().ToList();
        List<DateOnly> dates = pattern.Dates.Select(EventDates.Parse).OfType<DateOnly>().Distinct().Order().ToList();
        string? problem = pattern.Repeats switch
        {
            Repeats.Weekly or Repeats.EverySecondWeek when days.Count == 0 => "Choose the days it's on.",
            Repeats.MonthlyByWeekday when days.Count != 1 || pattern.WeekOfMonth is not (-1 or (>= 1 and <= 4)) => "Choose which week of the month and which day.",
            Repeats.MonthlyByDate when pattern.DayOfMonth is < 1 or > 31 => "Choose the date in the month.",
            Repeats.OnDates when dates.Count == 0 => "Choose its dates.",
            _ when !Enum.IsDefined(pattern.Repeats) => "Choose how it repeats.",
            _ => null,
        };
        if (problem is not null)
            throw Invalid(problem);
        return new RegularPattern
        {
            Repeats = pattern.Repeats,
            Days = days,
            WeekOfMonth = pattern.Repeats == Repeats.MonthlyByWeekday ? pattern.WeekOfMonth : 0,
            DayOfMonth = pattern.Repeats == Repeats.MonthlyByDate ? pattern.DayOfMonth : 0,
            Dates = pattern.Repeats == Repeats.OnDates ? dates.Select(EventDates.Format).ToList() : [],
            StartDate = EventDates.Format(pattern.Repeats == Repeats.OnDates ? dates[0] : start),
            EndDate = pattern.Repeats == Repeats.OnDates ? EventDates.Format(dates[^1]) : end is { } last ? EventDates.Format(last) : "",
        };
    }

    private static (Guid? Id, string Name, DateOnly Start, DateOnly End) CheckedBreak(BreakInfo info)
    {
        DateOnly start = EventDates.Parse(info.StartDate) ?? throw Invalid("Give each break its first date.");
        DateOnly end = EventDates.Parse(info.EndDate) ?? start;
        if (end < start)
            throw Invalid("A break can't end before it starts.");
        string name = info.Name.Trim();
        return (Guid.TryParse(info.Id, out Guid id) ? id : null, name.Length > 0 ? name : "Break", start, end);
    }

    private static (DateOnly, DateOnly?, int?, int?) Times(EventTimes times)
    {
        DateOnly start = EventDates.Parse(times.StartDate) ?? throw Invalid("Choose its date.");
        DateOnly? end = EventDates.Parse(times.EndDate);
        if (end < start)
            throw Invalid("It can't end before it starts.");
        (int? startMinute, int? endMinute) = Minutes(times.StartMinute, times.EndMinute);
        return (start, end == start ? null : end, startMinute, endMinute);
    }

    private static (int?, int?) Minutes(int? start, int? end)
    {
        if (start is < 0 or >= 24 * 60 || end is < 0 or > 24 * 60)
            throw Invalid("Choose a time of day.");
        return start is null ? (null, null) : (start, end);
    }

    private static bool Shown(Showing showing, DateTime? showFrom, DateTime now) =>
        showing == Showing.Shown || (showing == Showing.ShownFrom && showFrom is { } from && from.ToUniversalTime() <= now);

    private static DateTime? ShowFromOf(Showing showing, DateTime? showFrom) =>
        showing != Showing.ShownFrom ? null : Utc(showFrom) ?? throw Invalid("Choose the date it's shown from.");

    private static DateTime? Utc(DateTime? at) => at is { } value ? DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc) : null;

    private static bool IsWebAddress(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) && uri.Scheme is "https" or "http";

    private static Guid ParseId(string id) => Guid.TryParse(id, out Guid parsed) ? parsed : throw NotHere("That isn't here any more.");

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static RpcException NotHere(string message) => new(new Status(StatusCode.NotFound, message));

    private static RpcException Refused(string message) => new(new Status(StatusCode.PermissionDenied, message));
}
