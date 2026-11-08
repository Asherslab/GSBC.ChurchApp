using System.Globalization;
using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Hosts.Website;

public sealed record EventDay(DateOnly Date, IReadOnlyList<EventOccurrence> Events);

public sealed record EventWeek(DateOnly Start, string Heading, string Range, IReadOnlyList<EventDay> Days);

public sealed record RegularGathering(EventOccurrence Next, string When, string? Place, string? NotOn);

public static class EventListing
{
    public const int FirstWeeks = 8;
    public const int MostWeeks = 54;

    private static readonly CultureInfo English = CultureInfo.InvariantCulture;

    public static IReadOnlyList<CalendarInfo> Limited(IReadOnlyList<CalendarInfo> calendars, string? limit)
    {
        HashSet<string> slugs = (limit ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<CalendarInfo> chosen = calendars.Where(calendar => slugs.Contains(calendar.Slug)).ToList();
        return chosen.Count > 0 ? chosen : calendars;
    }

    public static IEnumerable<EventOccurrence> On(IEnumerable<EventOccurrence> events, IEnumerable<CalendarInfo> calendars)
    {
        HashSet<string> ids = calendars.Select(calendar => calendar.Id).ToHashSet();
        return events.Where(occurrence => ids.Contains(occurrence.CalendarId) || occurrence.AlsoOn.Any(ids.Contains));
    }

    public static IEnumerable<EventOccurrence> Ahead(IEnumerable<EventOccurrence> events, DateTime nowUtc)
    {
        DateOnly today = ChurchTime.DayOf(nowUtc);
        return events.Where(occurrence => occurrence.EndsAt >= nowUtc || (occurrence.NotOn && EventWords.DayOf(occurrence) >= today));
    }

    public static IEnumerable<EventOccurrence> Listed(IEnumerable<EventOccurrence> ahead) => ahead.Where(OccurrenceListing.IsListedOnTheWebsite);

    public static IReadOnlyList<RegularGathering> Gatherings(IEnumerable<EventOccurrence> ahead) => ahead
        .Where(occurrence => !OccurrenceListing.IsListedOnTheWebsite(occurrence))
        .GroupBy(occurrence => occurrence.ItemId)
        .Select(dates => (Next: dates.FirstOrDefault(date => !date.NotOn), Off: dates.FirstOrDefault(date => date.NotOn)))
        .Where(regular => regular.Next is not null)
        .Select(regular => new RegularGathering(
            regular.Next!,
            EventWords.Join(Sentence(regular.Next!.Repeats), EventWords.Times(regular.Next)),
            EventWords.PlaceLine(regular.Next),
            regular.Off is { } off ? $"Not on {EventWords.Date(EventWords.DayOf(off))}{(EventDates.Parse(off.BackOn) is { } back ? $", back on {EventWords.Date(back)}" : "")}{(off.NotOnReason.Length > 0 ? $" ({off.NotOnReason})" : "")}" : null))
        .OrderBy(regular => ChurchTime.ToChurch(regular.Next.StartsAt).DayOfWeek)
        .ThenBy(regular => ChurchTime.ToChurch(regular.Next.StartsAt).TimeOfDay)
        .ToList();

    public static IReadOnlyList<EventWeek> Weeks(IEnumerable<EventOccurrence> ahead, DateOnly today, int weeks)
    {
        DateOnly thisWeek = EventWords.SundayOf(today);
        DateOnly end = thisWeek.AddDays(7 * Math.Clamp(weeks, 1, MostWeeks));
        return ahead
            .Where(occurrence => EventWords.DayOf(occurrence) < end)
            .GroupBy(occurrence => EventWords.SundayOf(EventWords.DayOf(occurrence)))
            .OrderBy(week => week.Key)
            .Select(week => new EventWeek(
                week.Key,
                WeekHeading(week.Key, thisWeek),
                Range(week.Key, week.Key.AddDays(6)),
                week.GroupBy(EventWords.DayOf).OrderBy(day => day.Key).Select(day => new EventDay(day.Key, day.ToList())).ToList()))
            .ToList();
    }

    public static bool HasLater(IEnumerable<EventOccurrence> ahead, DateOnly today, int weeks) =>
        ahead.Any(occurrence => EventWords.DayOf(occurrence) >= EventWords.SundayOf(today).AddDays(7 * Math.Clamp(weeks, 1, MostWeeks)));

    public static IReadOnlyList<EventOccurrence> OtherDates(IEnumerable<EventOccurrence> events, EventOccurrence occurrence, DateTime nowUtc, int count) =>
        occurrence.Kind == OccurrenceKind.OneOff
            ? []
            : Ahead(events, nowUtc).Where(other => other.ItemId == occurrence.ItemId && other.Kind != OccurrenceKind.OneOff && other.Id != occurrence.Id).Take(count).ToList();

    public static bool IsQuiet(EventOccurrence occurrence) => occurrence.NotOn || occurrence.Kind == OccurrenceKind.Regular;

    private static string Sentence(string words) => words.Length > 0 ? char.ToUpperInvariant(words[0]) + words[1..] : words;

    public static string WeekHeading(DateOnly start, DateOnly thisWeek) => (start.DayNumber - thisWeek.DayNumber) switch
    {
        0 => "This week",
        7 => "Next week",
        _ => $"Week of {start.ToString("d MMMM", English)}",
    };

    public static string Range(DateOnly first, DateOnly last) =>
        first.Month == last.Month
            ? $"{first.Day} – {last.ToString("d MMMM", English)}"
            : $"{first.ToString("d MMMM", English)} – {last.ToString("d MMMM", English)}";
}
