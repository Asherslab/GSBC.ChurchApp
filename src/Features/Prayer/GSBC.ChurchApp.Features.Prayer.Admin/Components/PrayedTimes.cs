using System.Globalization;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Prayer.Admin.Components;

internal static class PrayedTimes
{
    public const int Midday = 12 * 60;
    private const int Hour = 60;
    private const int Day = 24 * Hour;
    private const int FirstDayTime = 7 * Hour;
    private const int LastDayTime = 21 * Hour;

    public static readonly IReadOnlyList<int> Choices = [5, 10, 15, 20, 30, 45, Hour, 90, 2 * Hour, 3 * Hour, 4 * Hour, 6 * Hour, 8 * Hour, 12 * Hour, Day, 2 * Day];

    public static readonly IReadOnlyList<FieldOption> DayTimes = Enumerable.Range(0, (LastDayTime - FirstDayTime) / 30)
        .Select(step => FirstDayTime + step * 30)
        .Select(minute => new FieldOption(minute.ToString(CultureInfo.InvariantCulture), Clock(minute)))
        .ToList();

    public static string Long(int minutes) => minutes switch
    {
        >= Day when minutes % Day == 0 => Plural(minutes / Day, "day"),
        >= Hour when minutes % Hour == 0 => Plural(minutes / Hour, "hour"),
        > Hour => $"{Plural(minutes / Hour, "hour")} {Plural(minutes % Hour, "minute")}",
        _ => Plural(minutes, "minute"),
    };

    public static string Short(int minutes) => minutes switch
    {
        >= Day when minutes % Day == 0 => $"{minutes / Day}d",
        >= Hour when minutes % Hour == 0 => $"{minutes / Hour}h",
        > Hour => $"{minutes / Hour}h {minutes % Hour}m",
        _ => $"{minutes}m",
    };

    public static string Clock(int minute)
    {
        int hour = minute / Hour, past = minute % Hour;
        string half = hour < 12 ? "am" : "pm";
        int shown = hour % 12 == 0 ? 12 : hour % 12;
        return past == 0 ? $"{shown}{half}" : $"{shown}:{past:00}{half}";
    }

    private static string Plural(int count, string unit) => count == 1 ? $"1 {unit}" : $"{count} {unit}s";
}
