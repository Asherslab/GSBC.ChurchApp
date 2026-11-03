using System.Globalization;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Platform.Ui;

public static class ChurchDates
{
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;

    public static string Describe(DateTime utc, DateLineStyle style, bool withTime, DateTime nowUtc, DateTime? untilUtc = null)
    {
        DateTime at = ChurchTime.ToChurch(utc);
        DateTime? until = untilUtc is { } end ? ChurchTime.ToChurch(end) : null;
        string day = style switch
        {
            DateLineStyle.TimeOnly => "",
            DateLineStyle.Relative => Relative(at.Date, ChurchTime.ToChurch(nowUtc).Date),
            DateLineStyle.Short => at.ToString("ddd d MMM", English),
            _ => at.ToString("dddd d MMMM", English),
        };
        if (style != DateLineStyle.TimeOnly && at.Year != ChurchTime.ToChurch(nowUtc).Year)
            day += at.ToString(" yyyy", English);

        if (until is { } last && last.Date != at.Date)
            return $"{day} – {last.ToString(style == DateLineStyle.Long ? "dddd d MMMM" : "ddd d MMM", English)}";
        if (!withTime && style != DateLineStyle.TimeOnly)
            return day;

        string time = until is { } finish ? $"{Time(at)}–{Time(finish)}" : Time(at);
        return day.Length == 0 ? time : $"{day} · {time}";
    }

    public static string Time(DateTime churchLocal)
    {
        string suffix = churchLocal.Hour < 12 ? "am" : "pm";
        int hour = churchLocal.Hour % 12 == 0 ? 12 : churchLocal.Hour % 12;
        return churchLocal.Minute == 0 ? $"{hour}{suffix}" : $"{hour}:{churchLocal.Minute:00}{suffix}";
    }

    public static string Relative(DateTime day, DateTime today) => (day - today).Days switch
    {
        0 => "Today",
        1 => "Tomorrow",
        -1 => "Yesterday",
        > 1 and < 7 => day.ToString("dddd", English),
        _ => day.ToString("ddd d MMM", English),
    };
}
