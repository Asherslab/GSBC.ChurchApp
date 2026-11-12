using System.Globalization;
using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Platform.Ui;

public static partial class TypedDates
{
    public const string DayFormat = "yyyy-MM-dd";
    public const string TimeFormat = "HH:mm";

    private static readonly CultureInfo English = CultureInfo.InvariantCulture;
    private static readonly CultureInfo Australian = CultureInfo.GetCultureInfo("en-AU");

    private static readonly string[] Months = ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"];
    private static readonly string[] Weekdays = ["sun", "mon", "tue", "wed", "thu", "fri", "sat"];

    public static DateOnly? Day(string? iso) =>
        DateOnly.TryParseExact(iso, DayFormat, English, DateTimeStyles.None, out DateOnly day) ? day : null;

    public static TimeOnly? Time(string? iso) =>
        TimeOnly.TryParseExact(iso, TimeFormat, English, DateTimeStyles.None, out TimeOnly time) ? time : null;

    public static string Iso(DateOnly day) => day.ToString(DayFormat, English);

    public static string Iso(TimeOnly time) => time.ToString(TimeFormat, English);

    public static string Show(DateOnly day) => day.ToString("ddd d MMM yyyy", English);

    public static string ShowInFull(DateOnly day) => day.ToString("dddd d MMMM yyyy", English);

    public static string Show(TimeOnly time) => ChurchDates.Time(DateTime.MinValue.Add(time.ToTimeSpan()));

    public static string Month(DateOnly firstOfMonth) => firstOfMonth.ToString("MMMM yyyy", English);

    public static DateOnly ThisSunday(DateOnly today) => today.AddDays(((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7);

    public static bool TryParseDay(string? text, DateOnly today, out DateOnly day)
    {
        day = default;
        string typed = Squash(text);
        if (typed.Length == 0)
            return false;
        if (DateOnly.TryParseExact(typed, DayFormat, English, DateTimeStyles.None, out day))
            return true;

        switch (typed)
        {
            case "today":
                day = today;
                return true;
            case "tomorrow":
                day = today.AddDays(1);
                return true;
            case "yesterday":
                day = today.AddDays(-1);
                return true;
        }

        if (Weekday().Match(typed) is { Success: true } named && WeekdayIndex(named.Groups["day"].Value) is { } weekday)
        {
            int ahead = (weekday - (int)today.DayOfWeek + 7) % 7;
            day = today.AddDays(ahead == 0 && named.Groups["next"].Value == "next" ? 7 : ahead);
            return true;
        }

        if (Numeric().Match(typed) is { Success: true } numeric)
            return Make(numeric.Groups["year"].Value, int.Parse(numeric.Groups["month"].Value, English), numeric.Groups["day"].Value, today, out day);

        if (DayThenMonth().Match(typed) is { Success: true } words && MonthIndex(words.Groups["month"].Value) is { } month)
            return Make(words.Groups["year"].Value, month, words.Groups["day"].Value, today, out day);

        if (MonthThenDay().Match(typed) is { Success: true } american && MonthIndex(american.Groups["month"].Value) is { } monthFirst)
            return Make(american.Groups["year"].Value, monthFirst, american.Groups["day"].Value, today, out day);

        return DateOnly.TryParse(typed, Australian, DateTimeStyles.None, out day);
    }

    public static bool TryParseTime(string? text, out TimeOnly time)
    {
        time = default;
        string typed = Squash(text).Replace(" ", "", StringComparison.Ordinal);
        switch (typed)
        {
            case "":
                return false;
            case "noon" or "midday":
                time = new TimeOnly(12, 0);
                return true;
            case "midnight":
                time = new TimeOnly(0, 0);
                return true;
        }

        if (Clock().Match(typed) is not { Success: true } clock)
            return false;
        string digits = clock.Groups["hour"].Value + clock.Groups["minute"].Value;
        int hour;
        int minute;
        if (clock.Groups["minute"].Success)
        {
            hour = int.Parse(clock.Groups["hour"].Value, English);
            minute = int.Parse(clock.Groups["minute"].Value, English);
        }
        else if (digits.Length >= 3)
        {
            hour = int.Parse(digits[..^2], English);
            minute = int.Parse(digits[^2..], English);
        }
        else
        {
            hour = int.Parse(digits, English);
            minute = 0;
        }

        string half = clock.Groups["half"].Value;
        if (half.Length > 0)
        {
            if (hour is < 1 or > 12)
                return false;
            hour = half.StartsWith('p') ? hour % 12 + 12 : hour % 12;
        }
        if (hour > 23 || minute > 59)
            return false;
        time = new TimeOnly(hour, minute);
        return true;
    }

    private static bool Make(string year, int month, string dayOfMonth, DateOnly today, out DateOnly day)
    {
        day = default;
        int number = year.Length switch
        {
            0 => today.Year,
            2 => 2000 + int.Parse(year, English),
            _ => int.Parse(year, English),
        };
        int date = int.Parse(dayOfMonth, English);
        if (month is < 1 or > 12 || date < 1 || date > DateTime.DaysInMonth(number, month))
            return false;
        day = new DateOnly(number, month, date);
        return true;
    }

    private static string Squash(string? text) =>
        Spaces().Replace((text ?? "").Trim().ToLowerInvariant().Replace(",", " ", StringComparison.Ordinal), " ");

    private static int? MonthIndex(string word) =>
        word.Length >= 3 && Array.FindIndex(Months, month => word.StartsWith(month, StringComparison.Ordinal)) is var index and >= 0 ? index + 1 : null;

    private static int? WeekdayIndex(string word) =>
        word.Length >= 2 && Array.FindIndex(Weekdays, weekday => weekday.StartsWith(word[..Math.Min(3, word.Length)], StringComparison.Ordinal)) is var index and >= 0 ? index : null;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"^(?:(?<next>next|this)\s)?(?<day>[a-z]{2,9})$")]
    private static partial Regex Weekday();

    [GeneratedRegex(@"^(?<day>\d{1,2})[/.\-](?<month>\d{1,2})(?:[/.\-](?<year>\d{2}|\d{4}))?$")]
    private static partial Regex Numeric();

    [GeneratedRegex(@"^(?:[a-z]{2,9}\s)?(?<day>\d{1,2})(?:st|nd|rd|th)?\s(?<month>[a-z]{3,9})(?:\s(?<year>\d{4}))?$")]
    private static partial Regex DayThenMonth();

    [GeneratedRegex(@"^(?:[a-z]{2,9}\s)?(?<month>[a-z]{3,9})\s(?<day>\d{1,2})(?:st|nd|rd|th)?(?:\s(?<year>\d{4}))?$")]
    private static partial Regex MonthThenDay();

    [GeneratedRegex(@"^(?<hour>\d{1,2})(?:[:.](?<minute>\d{2}))?(?<half>am|pm|a|p)?$|^(?<hour>\d{3,4})(?<half>am|pm|a|p)?$")]
    private static partial Regex Clock();
}
