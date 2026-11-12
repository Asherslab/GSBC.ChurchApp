using System.Globalization;

namespace GSBC.ChurchApp.Features.Rosters.Contracts;

public static class AwayRules
{
    public const string AwayFlag = "Away that day";
    public const string AwayThenFlag = "Away at that time";
    public const string DayFormat = "yyyy-MM-dd";
    public const string TimeFormat = "HH:mm";

    private static bool AllDay(AwaySpan span) => span.StartTime.Length == 0 && span.EndTime.Length == 0;

    public static bool Lasting(AwaySpan span) => span.Weekly && span.To.Length == 0;

    public static bool Current(AwaySpan span, DateOnly today) => span.To.Length == 0 || (Day(span.To) is { } to && to >= today);

    public static string? FlagFor(IEnumerable<AwaySpan> spans, DateTime startsLocal, DateTime endsLocal, bool allDay) =>
        spans.Where(span => Covers(span, startsLocal, endsLocal, allDay))
            .Select(span => AllDay(span) ? AwayFlag : AwayThenFlag)
            .OrderBy(flag => flag == AwayFlag ? 0 : 1)
            .FirstOrDefault();

    public static bool OnDay(AwaySpan span, DateOnly day) => InRun(span, day) && !Skips(span, day);

    public static bool Skips(AwaySpan span, DateOnly day) => InRun(span, day) && span.Except.Any(each => Day(each) == day);

    private static bool InRun(AwaySpan span, DateOnly day) =>
        Day(span.From) is { } from && day >= from
        && (span.To.Length == 0 ? span.Weekly : Day(span.To) is { } to && day <= to)
        && (!span.Weekly || span.Weekdays.Contains((int)day.DayOfWeek));

    public static bool Covers(AwaySpan span, DateTime startsLocal, DateTime endsLocal, bool allDay)
    {
        DateOnly day = DateOnly.FromDateTime(startsLocal);
        if (!OnDay(span, day))
            return false;
        if (allDay || AllDay(span))
            return true;

        TimeSpan awayFrom = Time(span.StartTime)?.ToTimeSpan() ?? TimeSpan.Zero;
        TimeSpan awayTo = Time(span.EndTime)?.ToTimeSpan() ?? WholeDay;
        TimeSpan starts = startsLocal.TimeOfDay;
        TimeSpan ends = DateOnly.FromDateTime(endsLocal) == day ? endsLocal.TimeOfDay : WholeDay;
        return ends <= starts ? awayFrom <= starts && starts < awayTo : starts < awayTo && ends > awayFrom;
    }

    private static readonly TimeSpan WholeDay = TimeSpan.FromDays(1);

    public static string? WhyNot(AwaySave save, DateOnly today)
    {
        if (Day(save.From) is not { } from)
            return "Choose the first day.";
        if (save.To.Length == 0 && !save.Weekly)
            return "Choose the last day.";
        if (save.To.Length > 0 && Day(save.To) is not { } to)
            return "Choose the last day.";
        if (save.To.Length > 0 && Day(save.To) < from)
            return "The last day can't be before the first.";
        if (save.To.Length > 0 && Day(save.To) < today)
            return "Those days have passed.";
        if (save.Weekly && (save.Weekdays.Count == 0 || save.Weekdays.Any(day => day is < 0 or > 6)))
            return "Choose the days of the week.";
        if (save.StartTime.Length > 0 && Time(save.StartTime) is null || save.EndTime.Length > 0 && Time(save.EndTime) is null)
            return "Choose the times.";
        if (Time(save.StartTime) is { } start && Time(save.EndTime) is { } end && end <= start)
            return "The time they're back has to be after the time they're away from.";
        if (save.Except.Any(each => Day(each) is null))
            return "Choose the days they're not away.";
        return null;
    }

    public static DateOnly? Day(string wire) =>
        DateOnly.TryParseExact(wire, DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day) ? day : null;

    public static TimeOnly? Time(string wire) =>
        TimeOnly.TryParseExact(wire, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly time) ? time : null;
}
