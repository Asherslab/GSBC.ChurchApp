using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Events.Api;

internal static class Expansion
{
    private const int LookingForBackOnDays = 400;

    public static IEnumerable<DateOnly> DatesOf(RegularPattern pattern, DateOnly from, DateOnly to)
    {
        DateOnly start = EventDates.Parse(pattern.StartDate) ?? DateOnly.MinValue;
        DateOnly end = EventDates.Parse(pattern.EndDate) ?? DateOnly.MaxValue;
        DateOnly first = start > from ? start : from;
        DateOnly last = end < to ? end : to;
        if (pattern.Repeats == Repeats.OnDates)
            return pattern.Dates.Select(EventDates.Parse).OfType<DateOnly>().Where(date => date >= from && date <= to).Distinct().Order();
        if (first > last)
            return [];

        return pattern.Repeats switch
        {
            Repeats.Weekly => Days(first, last).Where(date => pattern.Days.Contains(date.DayOfWeek)),
            Repeats.EverySecondWeek => Days(first, last).Where(date => pattern.Days.Contains(date.DayOfWeek) && WeeksBetween(start == DateOnly.MinValue ? first : start, date) % 2 == 0),
            Repeats.MonthlyByWeekday when pattern.Days.Count > 0 => Months(first, last).Select(month => NthWeekday(month, pattern.Days[0], pattern.WeekOfMonth)).OfType<DateOnly>().Where(date => date >= first && date <= last),
            Repeats.MonthlyByDate when pattern.DayOfMonth is >= 1 and <= 31 => Months(first, last).Where(month => pattern.DayOfMonth <= DateTime.DaysInMonth(month.Year, month.Month))
                .Select(month => new DateOnly(month.Year, month.Month, pattern.DayOfMonth)).Where(date => date >= first && date <= last),
            _ => [],
        };
    }

    public static IEnumerable<EventOccurrence> OccurrencesOf(
        RegularEventRecord regular, IReadOnlyDictionary<DateOnly, SpecialRecord> specials, IReadOnlyList<BreakRecord> breaks, DateOnly from, DateOnly to)
    {
        string repeats = RepeatWords.Of(regular.Pattern);
        foreach (DateOnly date in DatesOf(regular.Pattern, from, to))
        {
            SpecialRecord? special = specials.GetValueOrDefault(date);
            BreakRecord? inBreak = BreakOn(breaks, date);
            if (inBreak is not null && date.DayNumber - inBreak.StartDate.DayNumber >= EventsFeeds.NotOnShownForDays)
                continue;

            EventFields fields = special is null ? regular.Fields : FieldValues.Overlay(regular.Fields, special.Fields, special.Changed);
            bool ownTime = special is not null && special.Changed.Contains(EventFieldKeys.Time);
            int? startMinute = ownTime ? special!.StartMinute : regular.StartMinute;
            int? endMinute = ownTime ? special!.EndMinute : regular.EndMinute;
            bool notOn = inBreak is not null || special?.NotOn == true;

            EventOccurrence occurrence = new()
            {
                Id = EventDates.OccurrenceId(regular.Id, date),
                Kind = special is { Changed.Count: > 0 } ? OccurrenceKind.Special : OccurrenceKind.Regular,
                ItemId = regular.Id.ToString(),
                Date = EventDates.Format(date),
                CalendarId = regular.CalendarId.ToString(),
                Fields = fields,
                Repeats = repeats,
                NotOn = notOn,
                NotOnReason = inBreak?.Name ?? (special?.NotOn == true ? special.NotOnNote : ""),
                BackOn = notOn ? BackOn(regular, specials, breaks, date) : "",
                Missing = notOn ? [] : regular.Needs.Where(need => !FieldValues.IsSet(fields, need)).ToList(),
                KeptBy = regular.KeptBy,
                OpensAt = regular.OpensAt,
            };
            Time(occurrence, date, date, startMinute, endMinute);
            (occurrence.ChangedAt, occurrence.ChangeNote) = Latest((regular.ChangedAt, regular.ChangeNote), (special?.ChangedAt, special?.ChangeNote ?? ""));
            yield return occurrence;
        }
    }

    public static IEnumerable<RegularDate> RunOf(
        RegularEventRecord regular, IReadOnlyDictionary<DateOnly, SpecialRecord> specials, IReadOnlyList<BreakRecord> breaks, DateOnly from, DateOnly to)
    {
        foreach (DateOnly date in DatesOf(regular.Pattern, from, to))
        {
            SpecialRecord? special = specials.GetValueOrDefault(date);
            BreakRecord? inBreak = BreakOn(breaks, date);
            bool ownTime = special is not null && special.Changed.Contains(EventFieldKeys.Time);
            yield return new RegularDate
            {
                Date = EventDates.Format(date),
                OccurrenceId = EventDates.OccurrenceId(regular.Id, date),
                Title = special is not null && special.Changed.Contains(EventFieldKeys.Title) ? special.Fields.Title : regular.Fields.Title,
                StartMinute = ownTime ? special!.StartMinute : regular.StartMinute,
                EndMinute = ownTime ? special!.EndMinute : regular.EndMinute,
                Changed = special is { Changed.Count: > 0 },
                NotOn = inBreak is not null || special?.NotOn == true,
                NotOnReason = inBreak?.Name ?? (special?.NotOn == true ? special.NotOnNote : ""),
            };
        }
    }

    public static EventOccurrence OccurrenceOf(EventRecord record)
    {
        EventOccurrence occurrence = new()
        {
            Id = EventDates.OccurrenceId(record.Id),
            Kind = OccurrenceKind.OneOff,
            ItemId = record.Id.ToString(),
            Date = EventDates.Format(record.StartDate),
            EndDate = record.EndDate is { } end && end > record.StartDate ? EventDates.Format(end) : "",
            CalendarId = record.CalendarId.ToString(),
            Fields = record.Fields,
            NotOn = record.NotOn,
            NotOnReason = record.NotOnNote,
            ChangedAt = record.ChangedAt,
            ChangeNote = record.ChangeNote,
        };
        Time(occurrence, record.StartDate, record.EndDate ?? record.StartDate, record.StartMinute, record.EndMinute);
        return occurrence;
    }

    public static DateOnly LastDayOf(EventRecord record) => record.EndDate is { } end && end > record.StartDate ? end : record.StartDate;

    private static void Time(EventOccurrence occurrence, DateOnly firstDay, DateOnly lastDay, int? startMinute, int? endMinute)
    {
        if (startMinute is not { } start)
        {
            occurrence.AllDay = true;
            occurrence.StartsAt = ChurchTime.ToUtc(firstDay.ToDateTime(TimeOnly.MinValue));
            occurrence.EndsAt = ChurchTime.ToUtc(lastDay.AddDays(1).ToDateTime(TimeOnly.MinValue));
            return;
        }
        occurrence.StartsAt = ChurchTime.ToUtc(firstDay.ToDateTime(TimeOnly.MinValue).AddMinutes(start));
        DateTime ends = lastDay.ToDateTime(TimeOnly.MinValue).AddMinutes(endMinute ?? start);
        if (endMinute < start && lastDay == firstDay)
            ends = ends.AddDays(1);
        occurrence.EndsAt = ChurchTime.ToUtc(ends);
    }

    private static string BackOn(RegularEventRecord regular, IReadOnlyDictionary<DateOnly, SpecialRecord> specials, IReadOnlyList<BreakRecord> breaks, DateOnly date) =>
        DatesOf(regular.Pattern, date.AddDays(1), date.AddDays(LookingForBackOnDays))
            .FirstOrDefault(next => specials.GetValueOrDefault(next)?.NotOn != true && !breaks.Any(stop => stop.StartDate <= next && next <= stop.EndDate)) is { } back && back != default
            ? EventDates.Format(back)
            : "";

    private static BreakRecord? BreakOn(IReadOnlyList<BreakRecord> breaks, DateOnly date) =>
        breaks.Where(stop => stop.StartDate <= date && date <= stop.EndDate).MinBy(stop => stop.StartDate);

    private static (DateTime?, string) Latest((DateTime? At, string Note) first, (DateTime? At, string Note) second) =>
        second.At is { } secondAt && (first.At is null || secondAt > first.At) ? second : first;

    private static IEnumerable<DateOnly> Days(DateOnly first, DateOnly last)
    {
        for (DateOnly day = first; day <= last; day = day.AddDays(1))
            yield return day;
    }

    private static IEnumerable<DateOnly> Months(DateOnly first, DateOnly last)
    {
        for (DateOnly month = new(first.Year, first.Month, 1); month <= last; month = month.AddMonths(1))
            yield return month;
    }

    private static int WeeksBetween(DateOnly start, DateOnly date) =>
        Math.Abs(ChurchTime.WeekStart(date).DayNumber - ChurchTime.WeekStart(start).DayNumber) / 7;

    private static DateOnly? NthWeekday(DateOnly month, DayOfWeek day, int week)
    {
        if (week < 0)
        {
            DateOnly last = month.AddMonths(1).AddDays(-1);
            return last.AddDays(-(((int)last.DayOfWeek - (int)day + 7) % 7));
        }
        DateOnly first = month.AddDays(((int)day - (int)month.DayOfWeek + 7) % 7);
        DateOnly nth = first.AddDays(7 * (Math.Clamp(week, 1, 5) - 1));
        return nth.Month == month.Month ? nth : null;
    }
}
