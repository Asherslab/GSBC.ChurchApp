using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts;

namespace GSBC.ChurchApp.Features.Events.Api;

internal static class EditMerge
{
    public static EventSave Onto(EventSave current, EventSave wanted)
    {
        if (wanted.Base is not { } start)
            return wanted;
        Timed timed = Merged(Timed(current), Timed(start), Timed(wanted));
        bool datesChanged = (start.Times.StartDate, start.Times.EndDate) != (wanted.Times.StartDate, wanted.Times.EndDate);
        bool showingChanged = (start.Showing, start.ShowFrom) != (wanted.Showing, wanted.ShowFrom);
        bool notOnChanged = start.NotOn != wanted.NotOn;
        return new EventSave
        {
            CalendarId = Pick(current.CalendarId, start.CalendarId, wanted.CalendarId),
            EventId = wanted.EventId,
            NewId = wanted.NewId,
            Fields = timed.Fields,
            Times = new EventTimes
            {
                StartDate = datesChanged ? wanted.Times.StartDate : current.Times.StartDate,
                EndDate = datesChanged ? wanted.Times.EndDate : current.Times.EndDate,
                StartMinute = timed.StartMinute,
                EndMinute = timed.EndMinute,
            },
            Showing = showingChanged ? wanted.Showing : current.Showing,
            ShowFrom = showingChanged ? wanted.ShowFrom : current.ShowFrom,
            NotOn = notOnChanged ? wanted.NotOn : current.NotOn,
            NotOnNote = notOnChanged ? wanted.NotOnNote : current.NotOnNote,
        };
    }

    public static RegularEventSave Onto(RegularEventSave current, RegularEventSave wanted)
    {
        if (wanted.Base is not { } start)
            return wanted;
        Timed timed = Merged(Timed(current), Timed(start), Timed(wanted));
        bool showingChanged = (start.Showing, start.ShowFrom) != (wanted.Showing, wanted.ShowFrom);
        bool remindChanged = !start.RemindDaysBefore.SequenceEqual(wanted.RemindDaysBefore);
        return new RegularEventSave
        {
            CalendarId = Pick(current.CalendarId, start.CalendarId, wanted.CalendarId),
            RegularEventId = wanted.RegularEventId,
            NewId = wanted.NewId,
            Fields = timed.Fields,
            StartMinute = timed.StartMinute,
            EndMinute = timed.EndMinute,
            Pattern = Same(start.Pattern, wanted.Pattern) ? current.Pattern : wanted.Pattern,
            Showing = showingChanged ? wanted.Showing : current.Showing,
            ShowFrom = showingChanged ? wanted.ShowFrom : current.ShowFrom,
            Needs = start.Needs.SequenceEqual(wanted.Needs) ? current.Needs : wanted.Needs,
            RemindDaysBefore = remindChanged ? wanted.RemindDaysBefore : current.RemindDaysBefore,
            BreakIds = start.BreakIds.Order().SequenceEqual(wanted.BreakIds.Order()) ? current.BreakIds : wanted.BreakIds,
            OwnBreaks = start.OwnBreaks.Select(Key).SequenceEqual(wanted.OwnBreaks.Select(Key)) ? current.OwnBreaks : wanted.OwnBreaks,
            ListEveryDate = Pick(current.ListEveryDate, start.ListEveryDate, wanted.ListEveryDate),
            KeepOwnConfirmed = wanted.KeepOwnConfirmed,
        };
    }

    public static DateSave Onto(DateSave current, DateSave wanted)
    {
        if (wanted.Base is not { } start)
            return wanted;
        Timed timed = Merged(Timed(current), Timed(start), Timed(wanted));
        bool notOnChanged = start.NotOn != wanted.NotOn;
        return new DateSave
        {
            CalendarId = wanted.CalendarId,
            RegularEventId = wanted.RegularEventId,
            Date = wanted.Date,
            Fields = timed.Fields,
            StartMinute = timed.StartMinute,
            EndMinute = timed.EndMinute,
            NotOn = notOnChanged ? wanted.NotOn : current.NotOn,
            NotOnNote = notOnChanged ? wanted.NotOnNote : current.NotOnNote,
        };
    }

    private static Timed Merged(Timed current, Timed start, Timed wanted)
    {
        List<string> changed = FieldValues.Differences(start, wanted);
        bool timeChanged = changed.Contains(EventFieldKeys.Time);
        return new Timed(FieldValues.Overlay(current.Fields, wanted.Fields, changed),
            timeChanged ? wanted.StartMinute : current.StartMinute, timeChanged ? wanted.EndMinute : current.EndMinute);
    }

    private static T Pick<T>(T current, T start, T wanted) => EqualityComparer<T>.Default.Equals(start, wanted) ? current : wanted;

    private static bool Same(RegularPattern one, RegularPattern other) =>
        Fingerprint.Of(EventsContractsModel.Instance, new RegularEventSave { Pattern = one }) == Fingerprint.Of(EventsContractsModel.Instance, new RegularEventSave { Pattern = other });

    private static string Key(BreakInfo info) => $"{info.Id}|{info.Name}|{info.StartDate}|{info.EndDate}";

    private static Timed Timed(EventSave save) => new(save.Fields, save.Times.StartMinute, save.Times.EndMinute);

    private static Timed Timed(RegularEventSave save) => new(save.Fields, save.StartMinute, save.EndMinute);

    private static Timed Timed(DateSave save) => new(save.Fields, save.StartMinute, save.EndMinute);
}
