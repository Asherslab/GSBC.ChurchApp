using System.Globalization;
using System.Text;
using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client;

public static class EventFile
{
    public static string NameOf(EventOccurrence occurrence) => $"{CalendarLinks.SlugOf(occurrence.Fields.Title)}.ics";

    public static string Of(EventOccurrence occurrence, DateTime nowUtc)
    {
        StringBuilder text = new();
        Line(text, "BEGIN:VCALENDAR");
        Line(text, "VERSION:2.0");
        Line(text, "PRODID:-//Good Shepherd Baptist Church//Church app//EN");
        Line(text, "BEGIN:VEVENT");
        Line(text, $"UID:{occurrence.Id}@gsbc-church-app");
        Line(text, $"DTSTAMP:{Utc(nowUtc)}");
        if (occurrence.AllDay)
        {
            DateOnly first = EventWords.DayOf(occurrence);
            DateOnly last = EventDates.Parse(occurrence.EndDate) ?? first;
            Line(text, $"DTSTART;VALUE=DATE:{first:yyyyMMdd}");
            Line(text, $"DTEND;VALUE=DATE:{last.AddDays(1):yyyyMMdd}");
        }
        else
        {
            Line(text, $"DTSTART:{Utc(occurrence.StartsAt)}");
            Line(text, $"DTEND:{Utc(occurrence.EndsAt > occurrence.StartsAt ? occurrence.EndsAt : occurrence.StartsAt.AddHours(1))}");
        }
        Line(text, $"SUMMARY:{Escape(occurrence.Fields.Title)}");
        if (Where(occurrence.Fields.Place) is { Length: > 0 } where)
            Line(text, $"LOCATION:{Escape(where)}");
        if (occurrence.Fields.Description?.Markdown is { Length: > 0 } about)
            Line(text, $"DESCRIPTION:{Escape(about)}");
        Line(text, "END:VEVENT");
        Line(text, "END:VCALENDAR");
        return text.ToString();
    }

    private static string? Where(EventPlace? place) => place is null ? null : EventWords.Join(place.Name, place.Address);

    private static string Utc(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal).Replace("\r\n", "\\n", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);

    private static void Line(StringBuilder text, string line)
    {
        const int Longest = 74;
        for (int start = 0; start < line.Length; start += Longest)
            text.Append(start == 0 ? "" : " ").Append(line, start, Math.Min(Longest, line.Length - start)).Append("\r\n");
    }
}
