using System.Globalization;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class RepeatWords
{
    private static readonly string[] Ordinals = ["", "first", "second", "third", "fourth"];

    public static string Of(RegularPattern pattern) => pattern.Repeats switch
    {
        Repeats.Weekly => $"every {Days(pattern.Days)}",
        Repeats.EverySecondWeek => $"every second {Days(pattern.Days)}",
        Repeats.MonthlyByWeekday => $"{(pattern.WeekOfMonth < 0 ? "last" : Ordinals[Math.Clamp(pattern.WeekOfMonth, 1, 4)])} {Days(pattern.Days.Take(1))} of the month",
        Repeats.MonthlyByDate => $"the {Nth(pattern.DayOfMonth)} of each month",
        Repeats.OnDates => pattern.Dates.Count == 1 ? "on 1 date" : $"on {pattern.Dates.Count} dates",
        _ => "",
    };

    private static string Days(IEnumerable<DayOfWeek> days)
    {
        List<string> names = days.Distinct().OrderBy(day => ((int)day + 6) % 7)
            .Select(day => CultureInfo.InvariantCulture.DateTimeFormat.GetDayName(day)).ToList();
        return names.Count switch
        {
            0 => "week",
            1 => names[0],
            _ => $"{string.Join(", ", names[..^1])} and {names[^1]}",
        };
    }

    private static string Nth(int day) =>
        day + (day % 100 is 11 or 12 or 13 ? "th" : (day % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" });
}
