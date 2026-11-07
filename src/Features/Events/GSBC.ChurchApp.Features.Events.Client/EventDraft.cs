using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed record EditTarget(string CalendarId, string? EventId = null, string? RegularEventId = null, DateOnly? Date = null)
{
    public bool IsNew => EventId is null && RegularEventId is null;

    public static EditTarget Of(EventOccurrence occurrence) => occurrence.Kind == OccurrenceKind.OneOff
        ? new EditTarget(occurrence.CalendarId, EventId: occurrence.ItemId)
        : new EditTarget(occurrence.CalendarId, RegularEventId: occurrence.ItemId, Date: EventWords.DayOf(occurrence));

    public static EditTarget New(string calendarId) => new(calendarId);
}

public enum DraftScope
{
    ThisDate,
    Every,
}

public sealed class TimedFields
{
    public EventFields Fields { get; set; } = new();
    public int? StartMinute { get; set; }
    public int? EndMinute { get; set; }
}

public sealed class EventDraft
{
    public const int DefaultStart = 10 * 60;
    public const int DefaultLength = 60;

    public string CalendarId { get; set; } = "";
    public string? EventId { get; private init; }
    public string? RegularEventId { get; private init; }
    public DateOnly? Date { get; private init; }
    public bool Repeating { get; set; }
    public DraftScope Scope { get; set; } = DraftScope.Every;

    public TimedFields Every { get; private init; } = new();
    public TimedFields ThisDate { get; private init; } = new();

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public RegularPattern Pattern { get; set; } = new();
    public List<string> Needs { get; set; } = [];
    public List<int> RemindDaysBefore { get; set; } = [7, 2];
    public List<string> BreakIds { get; set; } = [];
    public List<BreakInfo> OwnBreaks { get; set; } = [];
    public Showing Showing { get; set; } = Showing.Shown;
    public DateTime? ShowFrom { get; set; }
    public bool Asked { get; set; }
    public bool WasShown { get; private set; }
    public bool NotOnThisDate { get; set; }
    public string RepeatWords { get; private set; } = "";
    public string ChangeNote { get; set; } = "";

    public bool IsNew => EventId is null && RegularEventId is null;

    public bool IsDateOfRegular => RegularEventId is not null && Date is not null;

    public TimedFields Current => IsDateOfRegular && Scope == DraftScope.ThisDate ? ThisDate : Every;

    public EventFields Fields => Current.Fields;

    public static EventDraft New(string calendarId, DateOnly day) => new()
    {
        CalendarId = calendarId,
        StartDate = day,
        Every = new TimedFields { StartMinute = DefaultStart, EndMinute = DefaultStart + DefaultLength },
        Pattern = new RegularPattern { Repeats = Repeats.Weekly, Days = [day.DayOfWeek], StartDate = EventDates.Format(day) },
        Showing = Showing.Shown,
    };

    public static EventDraft From(EventDetail detail, DateTime nowUtc) => new()
    {
        CalendarId = detail.CalendarId,
        EventId = detail.Id,
        Every = new TimedFields { Fields = Copy(detail.Fields), StartMinute = detail.Times.StartMinute, EndMinute = detail.Times.EndMinute },
        StartDate = EventDates.Parse(detail.Times.StartDate) ?? ChurchTime.DayOf(nowUtc),
        EndDate = EventDates.Parse(detail.Times.EndDate),
        Showing = detail.Showing,
        ShowFrom = detail.ShowFrom,
        Asked = detail.Asked,
        WasShown = IsShown(detail.Showing, detail.ShowFrom, nowUtc),
        NotOnThisDate = detail.NotOn,
        ChangeNote = detail.ChangeNote,
    };

    public static EventDraft From(RegularEventDetail detail, DateOnly? date, DateTime nowUtc)
    {
        SpecialInfo? special = date is { } day ? detail.Specials.FirstOrDefault(entry => entry.Date == EventDates.Format(day)) : null;
        return new EventDraft
        {
            CalendarId = detail.CalendarId,
            RegularEventId = detail.Id,
            Date = date,
            Repeating = true,
            Scope = date is null ? DraftScope.Every : DraftScope.ThisDate,
            Every = new TimedFields { Fields = Copy(detail.Fields), StartMinute = detail.StartMinute, EndMinute = detail.EndMinute },
            ThisDate = OnDate(detail, special),
            StartDate = EventDates.Parse(detail.Pattern.StartDate) ?? date ?? ChurchTime.DayOf(nowUtc),
            Pattern = Copy(detail.Pattern),
            Needs = [.. detail.Needs],
            RemindDaysBefore = [.. detail.RemindDaysBefore],
            BreakIds = [.. detail.BreakIds],
            OwnBreaks = detail.OwnBreaks.Select(Copy).ToList(),
            Showing = detail.Showing,
            ShowFrom = detail.ShowFrom,
            Asked = detail.Asked,
            WasShown = IsShown(detail.Showing, detail.ShowFrom, nowUtc),
            NotOnThisDate = special?.NotOn == true,
            RepeatWords = detail.Repeats.Length > 0 ? detail.Repeats : Contracts.RepeatWords.Of(detail.Pattern),
            ChangeNote = special?.ChangeNote is { Length: > 0 } note ? note : detail.ChangeNote,
        };
    }

    public string RepeatsNow => Repeating ? Contracts.RepeatWords.Of(Pattern) : "";

    public string EveryLabel => $"Every date, {(RepeatWords.Length > 0 ? RepeatWords : RepeatsNow)}";

    public DateOnly ShownDay => Date ?? StartDate;

    public string WhenLine
    {
        get
        {
            string times = EventDates.Times(Current.StartMinute, Current.EndMinute);
            if (!Repeating && EndDate is { } last && last != StartDate)
                times = Current.StartMinute is null ? $"Until {EventWords.Date(last)}" : $"{times}, until {EventWords.Date(last)}";
            return Repeating ? EventWords.Join(times, RepeatsNow) : times;
        }
    }

    public EventSave ToEventSave() => new()
    {
        CalendarId = CalendarId,
        EventId = EventId ?? "",
        Fields = Every.Fields,
        Times = new EventTimes
        {
            StartDate = EventDates.Format(StartDate),
            EndDate = EndDate is { } end && end != StartDate ? EventDates.Format(end) : "",
            StartMinute = Every.StartMinute,
            EndMinute = Every.StartMinute is null ? null : Every.EndMinute,
        },
        Showing = Showing,
        ShowFrom = Showing == Showing.ShownFrom ? ShowFrom : null,
        NotOn = NotOnThisDate,
    };

    public RegularEventSave ToRegularSave(bool keepOwnConfirmed)
    {
        Pattern.StartDate = Pattern.StartDate.Length > 0 ? Pattern.StartDate : EventDates.Format(StartDate);
        return new RegularEventSave
        {
            CalendarId = CalendarId,
            RegularEventId = RegularEventId ?? "",
            Fields = Every.Fields,
            Pattern = Pattern,
            StartMinute = Every.StartMinute,
            EndMinute = Every.StartMinute is null ? null : Every.EndMinute,
            Showing = Showing,
            ShowFrom = Showing == Showing.ShownFrom ? ShowFrom : null,
            Needs = Needs,
            RemindDaysBefore = RemindDaysBefore,
            BreakIds = BreakIds,
            OwnBreaks = OwnBreaks,
            KeepOwnConfirmed = keepOwnConfirmed,
        };
    }

    public DateSave ToDateSave(bool notOn) => new()
    {
        CalendarId = CalendarId,
        RegularEventId = RegularEventId ?? "",
        Date = EventDates.Format(Date ?? StartDate),
        Fields = ThisDate.Fields,
        StartMinute = ThisDate.StartMinute,
        EndMinute = ThisDate.StartMinute is null ? null : ThisDate.EndMinute,
        NotOn = notOn,
        NotOnNote = notOn ? "Not on this date" : "",
    };

    public string OccurrenceId(RegularEventDetail? saved = null)
    {
        if (EventId is { } eventId && Guid.TryParse(eventId, out Guid id))
            return EventDates.OccurrenceId(id);
        if (Guid.TryParse(saved?.Id ?? RegularEventId, out Guid regular))
        {
            DateOnly day = Date ?? (saved?.NextDates.Select(EventDates.Parse).FirstOrDefault(found => found is not null) ?? StartDate);
            return EventDates.OccurrenceId(regular, day);
        }
        return "";
    }

    private static TimedFields OnDate(RegularEventDetail detail, SpecialInfo? special)
    {
        EventFields fields = Copy(detail.Fields);
        if (special is null)
            return new TimedFields { Fields = fields, StartMinute = detail.StartMinute, EndMinute = detail.EndMinute };
        foreach (string key in special.Changed)
        {
            switch (key)
            {
                case EventFieldKeys.Title: fields.Title = special.Fields.Title; break;
                case EventFieldKeys.Label: fields.Label = special.Fields.Label; break;
                case EventFieldKeys.Picture: fields.Picture = special.Fields.Picture; break;
                case EventFieldKeys.Description: fields.Description = special.Fields.Description; break;
                case EventFieldKeys.Place: fields.Place = special.Fields.Place; break;
                case EventFieldKeys.Contacts: fields.Contacts = special.Fields.Contacts.Select(Copy).ToList(); break;
                case EventFieldKeys.Register: fields.RegisterLink = special.Fields.RegisterLink; break;
                case EventFieldKeys.Links: fields.Links = special.Fields.Links.Select(Copy).ToList(); break;
            }
        }
        bool timeChanged = special.Changed.Contains(EventFieldKeys.Time);
        return new TimedFields
        {
            Fields = fields,
            StartMinute = timeChanged ? special.StartMinute : detail.StartMinute,
            EndMinute = timeChanged ? special.EndMinute : detail.EndMinute,
        };
    }

    private static bool IsShown(Showing showing, DateTime? showFrom, DateTime nowUtc) =>
        showing == Showing.Shown || (showing == Showing.ShownFrom && showFrom is { } from && from <= nowUtc);

    public static EventFields Copy(EventFields fields) => new()
    {
        Title = fields.Title,
        Label = fields.Label,
        Picture = fields.Picture,
        Description = fields.Description,
        Place = fields.Place is { } place ? new EventPlace { PlaceId = place.PlaceId, Name = place.Name, Address = place.Address } : null,
        Contacts = fields.Contacts.Select(Copy).ToList(),
        RegisterLink = fields.RegisterLink,
        Links = fields.Links.Select(Copy).ToList(),
    };

    private static EventContact Copy(EventContact contact) => new() { PersonId = contact.PersonId, Name = contact.Name, Phone = contact.Phone, Email = contact.Email };

    private static EventLink Copy(EventLink link) => new() { Title = link.Title, Url = link.Url };

    private static BreakInfo Copy(BreakInfo info) => new() { Id = info.Id, Name = info.Name, StartDate = info.StartDate, EndDate = info.EndDate };

    private static RegularPattern Copy(RegularPattern pattern) => new()
    {
        Repeats = pattern.Repeats,
        Days = [.. pattern.Days],
        WeekOfMonth = pattern.WeekOfMonth,
        DayOfMonth = pattern.DayOfMonth,
        Dates = [.. pattern.Dates],
        StartDate = pattern.StartDate,
        EndDate = pattern.EndDate,
    };
}
