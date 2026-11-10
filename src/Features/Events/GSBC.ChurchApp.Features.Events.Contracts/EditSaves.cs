using System.Diagnostics.CodeAnalysis;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class EditSaves
{
    public static EventSave SaveOf(EventDetail detail) => new()
    {
        CalendarId = detail.CalendarId,
        EventId = detail.Id,
        Fields = detail.Fields,
        Times = detail.Times,
        Showing = detail.Showing,
        ShowFrom = detail.ShowFrom,
        NotOn = detail.NotOn,
        NotOnNote = detail.NotOnNote,
    };

    public static RegularEventSave SaveOf(RegularEventDetail detail) => new()
    {
        CalendarId = detail.CalendarId,
        RegularEventId = detail.Id,
        Fields = detail.Fields,
        Pattern = detail.Pattern,
        StartMinute = detail.StartMinute,
        EndMinute = detail.EndMinute,
        Showing = detail.Showing,
        ShowFrom = detail.ShowFrom,
        Needs = detail.Needs,
        RemindDaysBefore = detail.RemindDaysBefore,
        ListEveryDate = detail.ListEveryDate,
        BreakIds = detail.BreakIds,
        OwnBreaks = detail.OwnBreaks,
    };

    public static DateSave SaveOf(RegularEventDetail detail, string date)
    {
        Timed every = new(detail.Fields, detail.StartMinute, detail.EndMinute);
        SpecialInfo? special = detail.Specials.FirstOrDefault(entry => entry.Date == date);
        Timed onDate = special is null ? every : KeptValues.Effective(every, special);
        return new DateSave
        {
            CalendarId = detail.CalendarId,
            RegularEventId = detail.Id,
            Date = date,
            Fields = onDate.Fields,
            StartMinute = onDate.StartMinute,
            EndMinute = onDate.EndMinute,
            NotOn = special?.NotOn == true,
            NotOnNote = special?.NotOnNote ?? "",
        };
    }

    public static EventDetail Applied(EventDetail? stored, EventSave queued)
    {
        EventSave save = stored is null ? queued : EditMerge.Onto(SaveOf(stored), queued);
        EventDetail detail = stored is null ? new EventDetail { Id = queued.EventId.Length > 0 ? queued.EventId : queued.NewId } : Copy(stored);
        detail.CalendarId = save.CalendarId;
        detail.Fields = save.Fields;
        detail.Times = save.Times;
        detail.Showing = save.Showing;
        detail.ShowFrom = save.ShowFrom;
        detail.NotOn = save.NotOn;
        detail.NotOnNote = save.NotOnNote;
        return detail;
    }

    public static RegularEventDetail Applied(RegularEventDetail? stored, RegularEventSave queued)
    {
        RegularEventSave save = stored is null ? queued : EditMerge.Onto(SaveOf(stored), queued);
        RegularEventDetail detail = stored is null
            ? new RegularEventDetail { Id = queued.RegularEventId.Length > 0 ? queued.RegularEventId : queued.NewId }
            : Copy(stored);
        detail.CalendarId = save.CalendarId;
        detail.Fields = save.Fields;
        detail.Pattern = save.Pattern;
        detail.StartMinute = save.StartMinute;
        detail.EndMinute = save.EndMinute;
        detail.Showing = save.Showing;
        detail.ShowFrom = save.ShowFrom;
        detail.Needs = save.Needs;
        detail.RemindDaysBefore = save.RemindDaysBefore;
        detail.ListEveryDate = save.ListEveryDate;
        detail.BreakIds = save.BreakIds;
        detail.OwnBreaks = save.OwnBreaks;
        detail.Repeats = RepeatWords.Of(save.Pattern);
        return detail;
    }

    public static RegularEventDetail Applied(RegularEventDetail stored, DateSave queued)
    {
        DateSave save = EditMerge.Onto(SaveOf(stored, queued.Date), queued);
        RegularEventDetail detail = Copy(stored);
        List<string> changed = FieldValues.Differences(new Timed(detail.Fields, detail.StartMinute, detail.EndMinute), new Timed(save.Fields, save.StartMinute, save.EndMinute));
        detail.Specials.RemoveAll(special => special.Date == save.Date);
        if (changed.Count > 0 || save.NotOn)
            detail.Specials.Add(new SpecialInfo
            {
                Date = save.Date, Changed = changed, Fields = save.Fields, StartMinute = save.StartMinute, EndMinute = save.EndMinute,
                NotOn = save.NotOn, NotOnNote = save.NotOnNote,
            });
        detail.Specials.Sort((one, other) => string.CompareOrdinal(one.Date, other.Date));
        foreach (RegularDate date in detail.Dates.Where(date => date.Date == save.Date))
        {
            date.Changed = changed.Count > 0;
            date.NotOn = save.NotOn;
            date.Title = save.Fields.Title;
            (date.StartMinute, date.EndMinute) = (save.StartMinute, save.EndMinute);
        }
        return detail;
    }

    public static T Copy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T message)
    {
        using MemoryStream stream = new();
        EventsContractsModel.Instance.Serialize(stream, message);
        stream.Position = 0;
        return EventsContractsModel.Instance.Deserialize<T>(stream);
    }
}
