using System.Globalization;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class EventDates
{
    private const string IsoDate = "yyyy-MM-dd";

    public static string Format(DateOnly date) => date.ToString(IsoDate, CultureInfo.InvariantCulture);

    public static DateOnly? Parse(string? text) =>
        DateOnly.TryParseExact(text, IsoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : null;

    public static string OccurrenceId(Guid regularEventId, DateOnly date) => $"{regularEventId:N}-{Format(date)}";

    public static string OccurrenceId(Guid eventId) => eventId.ToString("N");

    public static bool TryReadOccurrenceId(string id, out Guid itemId, out DateOnly? date)
    {
        date = null;
        if (id.Length == 32 && Guid.TryParseExact(id, "N", out itemId))
            return true;
        if (id.Length == 43 && id[32] == '-' && Guid.TryParseExact(id[..32], "N", out itemId) && Parse(id[33..]) is { } day)
        {
            date = day;
            return true;
        }
        itemId = Guid.Empty;
        return false;
    }

    public static string Time(int minutes)
    {
        int hour = minutes / 60 % 24;
        int minute = minutes % 60;
        string suffix = hour < 12 ? "am" : "pm";
        int twelve = hour % 12 == 0 ? 12 : hour % 12;
        return minute == 0 ? $"{twelve}{suffix}" : $"{twelve}:{minute:00}{suffix}";
    }

    public static string Times(int? start, int? end) =>
        start is not { } from ? "All day" : end is { } to ? $"{Time(from)}–{Time(to)}" : Time(from);
}
