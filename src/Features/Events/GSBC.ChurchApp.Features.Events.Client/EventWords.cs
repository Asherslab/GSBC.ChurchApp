using System.Globalization;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Events.Client;

public static class EventWords
{
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;

    public static DateOnly Today(DateTime nowUtc) => ChurchTime.DayOf(nowUtc);

    public static DateOnly DayOf(EventOccurrence occurrence) => EventDates.Parse(occurrence.Date) ?? ChurchTime.DayOf(occurrence.StartsAt);

    public static DateOnly SundayOf(DateOnly day) => day.AddDays(-(int)day.DayOfWeek);

    public static string TimeOf(EventOccurrence occurrence) =>
        occurrence.AllDay ? "All day" : ChurchDates.Time(ChurchTime.ToChurch(occurrence.StartsAt));

    public static string Times(EventOccurrence occurrence)
    {
        if (occurrence.AllDay)
            return EndDay(occurrence) is { } last && last != DayOf(occurrence) ? $"Until {Date(last)}" : "All day";
        DateTime start = ChurchTime.ToChurch(occurrence.StartsAt), end = ChurchTime.ToChurch(occurrence.EndsAt);
        if (end <= start)
            return ChurchDates.Time(start);
        if (end.Date != start.Date)
            return $"{ChurchDates.Time(start)} to {end.ToString("ddd d MMM", English)}, {ChurchDates.Time(end)}";
        return Span(ChurchDates.Time(start), ChurchDates.Time(end));
    }

    public static string WhenLine(EventOccurrence occurrence) =>
        Join(Times(occurrence), occurrence.Kind == OccurrenceKind.OneOff ? null : occurrence.Repeats);

    public static string DayHeading(DateOnly day, DateOnly today) => (day.DayNumber - today.DayNumber) switch
    {
        0 => "Today",
        1 => "Tomorrow",
        _ => day.ToString("dddd", English),
    };

    public static string Date(DateOnly day) => day.ToString("d MMMM", English);

    public static string LongDate(DateOnly day) => day.ToString("dddd d MMMM", English);

    public static string Range(DateOnly first, DateOnly last) =>
        first.Month == last.Month
            ? $"{first.ToString("dddd d", English)} – {last.ToString("dddd d MMMM", English)}"
            : $"{first.ToString("dddd d MMMM", English)} – {last.ToString("dddd d MMMM", English)}";

    public static string NotOnLine(EventOccurrence occurrence)
    {
        string why = occurrence.NotOnReason.Length > 0 ? occurrence.NotOnReason : "Not on this week";
        return EventDates.Parse(occurrence.BackOn) is { } back ? $"{why} · back on {Date(back)}" : why;
    }

    public static string? PlaceLine(EventOccurrence occurrence) => occurrence.Fields.Place switch
    {
        { Name.Length: > 0 } place => place.Name,
        { Address.Length: > 0 } place => place.Address,
        _ when occurrence.Missing.Contains(EventFieldKeys.Place) => EventFieldKeys.Missing(EventFieldKeys.Place),
        _ => null,
    };

    public static string LabelOf(EventOccurrence occurrence, CalendarInfo? calendar) =>
        occurrence.Fields.Label.Length > 0 ? occurrence.Fields.Label : calendar?.Name ?? "";

    public static string Missing(IEnumerable<string> keys) => string.Join(" · ", keys.Select(EventFieldKeys.Missing));

    public static string NeedsLine(IEnumerable<string> keys)
    {
        List<string> names = keys.Select(Need).ToList();
        return names.Count == 0 ? "" : $"Needs {string.Join(" and ", names)}";
    }

    public static string Need(string key) => key switch
    {
        EventFieldKeys.Place => "a place",
        EventFieldKeys.Contacts => "a contact",
        EventFieldKeys.Description => "details",
        EventFieldKeys.Picture => "a picture",
        EventFieldKeys.Register => "a register link",
        _ => key,
    };

    public static string Span(string from, string to)
    {
        string fromSuffix = from[^2..], toSuffix = to[^2..];
        return fromSuffix == toSuffix ? $"{from[..^2]}–{to}" : $"{from}–{to}";
    }

    public static string Join(params string?[] parts) => string.Join(" · ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));

    private static DateOnly? EndDay(EventOccurrence occurrence) => EventDates.Parse(occurrence.EndDate);
}
