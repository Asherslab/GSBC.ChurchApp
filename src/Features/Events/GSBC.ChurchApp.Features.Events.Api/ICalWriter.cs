using System.Globalization;
using System.Text;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Events.Api;

internal static class ICalWriter
{
    private const int LongestLineOctets = 75;
    private const string UidHost = "calendar.gsbc-churchapp";
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromHours(1);

    public static string Write(string name, IEnumerable<EventOccurrence> occurrences, DateTime now)
    {
        StringBuilder text = new();
        Line(text, "BEGIN:VCALENDAR");
        Line(text, "VERSION:2.0");
        Line(text, "PRODID:-//GSBC Church App//Church calendar//EN");
        Line(text, "CALSCALE:GREGORIAN");
        Line(text, "METHOD:PUBLISH");
        Line(text, $"X-WR-CALNAME:{Escape(name)}");
        Line(text, $"X-WR-TIMEZONE:{ChurchTime.ZoneId}");
        Line(text, $"REFRESH-INTERVAL;VALUE=DURATION:{Duration(RefreshEvery)}");
        Line(text, $"X-PUBLISHED-TTL:{Duration(RefreshEvery)}");
        foreach (EventOccurrence occurrence in occurrences)
            Event(text, occurrence, now);
        Line(text, "END:VCALENDAR");
        return text.ToString();
    }

    private static void Event(StringBuilder text, EventOccurrence occurrence, DateTime now)
    {
        Line(text, "BEGIN:VEVENT");
        Line(text, $"UID:{occurrence.Id}@{UidHost}");
        Line(text, $"DTSTAMP:{Utc(now)}");
        if (occurrence.AllDay)
        {
            DateOnly first = EventDates.Parse(occurrence.Date) ?? ChurchTime.DayOf(occurrence.StartsAt);
            DateOnly last = EventDates.Parse(occurrence.EndDate) ?? first;
            Line(text, $"DTSTART;VALUE=DATE:{Day(first)}");
            Line(text, $"DTEND;VALUE=DATE:{Day(last.AddDays(1))}");
        }
        else
        {
            Line(text, $"DTSTART:{Utc(occurrence.StartsAt)}");
            Line(text, $"DTEND:{Utc(occurrence.EndsAt > occurrence.StartsAt ? occurrence.EndsAt : occurrence.StartsAt)}");
        }
        Line(text, $"SUMMARY:{Escape(occurrence.Fields.Title)}");
        if (Place(occurrence.Fields.Place) is { Length: > 0 } place)
            Line(text, $"LOCATION:{Escape(place)}");
        if (Description(occurrence) is { Length: > 0 } description)
            Line(text, $"DESCRIPTION:{Escape(description)}");
        if (Uri.TryCreate(occurrence.Fields.RegisterLink, UriKind.Absolute, out Uri? register))
            Line(text, $"URL:{register}");
        if (occurrence.Fields.Label.Length > 0)
            Line(text, $"CATEGORIES:{Escape(occurrence.Fields.Label)}");
        if (occurrence.ChangedAt is { } changed)
            Line(text, $"LAST-MODIFIED:{Utc(changed)}");
        Line(text, "END:VEVENT");
    }

    private static string Place(EventPlace? place) =>
        place is null ? "" : string.Join(", ", new[] { place.Name, place.Address }.Where(part => part.Length > 0).Distinct());

    private static string Description(EventOccurrence occurrence)
    {
        List<string> parts = [];
        if (occurrence.Fields.Description is { IsEmpty: false } description)
            parts.Add(description.Markdown.Trim());
        parts.AddRange(occurrence.Missing.Where(need => need != EventFieldKeys.Picture).Select(EventFieldKeys.Missing));
        if (occurrence.Fields.RegisterLink.Length > 0)
            parts.Add($"Register: {occurrence.Fields.RegisterLink}");
        parts.AddRange(occurrence.Fields.Links.Where(link => link.Url.Length > 0).Select(link => link.Title.Length > 0 ? $"{link.Title}: {link.Url}" : link.Url));
        return string.Join("\n\n", parts);
    }

    public static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace(";", "\\;", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal)
        .Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace("\r", "\n", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);

    private static string Utc(DateTime utc) => utc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Day(DateOnly date) => date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    private static string Duration(TimeSpan span) => $"PT{(int)span.TotalHours}H";

    private static void Line(StringBuilder text, string line)
    {
        int octets = 0;
        foreach (Rune rune in line.EnumerateRunes())
        {
            int width = rune.Utf8SequenceLength;
            if (octets + width > LongestLineOctets)
            {
                text.Append("\r\n ");
                octets = 1;
            }
            text.Append(rune.ToString());
            octets += width;
        }
        text.Append("\r\n");
    }
}
