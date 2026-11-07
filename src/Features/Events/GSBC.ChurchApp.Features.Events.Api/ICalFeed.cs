using System.Globalization;
using System.Text;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed record ICalTime(DateTime Value, bool IsDate, TimeZoneInfo? Zone)
{
    public DateTime ToChurch() => IsDate || Zone is null
        ? Value
        : ChurchTime.ToChurch(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(Value, DateTimeKind.Unspecified), Zone));

    public string Key => IsDate ? Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture) : ChurchTime.ToUtc(ToChurch()).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    public ICalTime At(DateTime value) => this with { Value = value };
}

internal sealed record ICalRule(string Frequency, int Interval, int? Count, ICalTime? Until, IReadOnlyList<(int Ordinal, DayOfWeek Day)> ByDay, IReadOnlyList<int> ByMonthDay, IReadOnlyList<int> ByMonth);

internal sealed class ICalEvent
{
    public string Uid { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public string Url { get; set; } = "";
    public bool Cancelled { get; set; }
    public ICalTime? Start { get; set; }
    public ICalTime? End { get; set; }
    public TimeSpan? Duration { get; set; }
    public ICalRule? Rule { get; set; }
    public List<ICalTime> RDates { get; } = [];
    public List<ICalTime> ExDates { get; } = [];
    public ICalTime? RecurrenceId { get; set; }
}

internal sealed record CopiedEvent(string SourceUid, ICalEvent Source, DateTime Start, DateTime End, bool AllDay);

internal static class ICalFeed
{
    private const int MostInstances = 2_000;
    private const int MostPeriods = 5_000;

    public static IReadOnlyList<ICalEvent> Read(string text)
    {
        if (!text.TrimStart('﻿', ' ', '\r', '\n', '\t').StartsWith("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("That address isn't an iCal calendar.");

        List<ICalEvent> events = [];
        ICalEvent? current = null;
        int nested = 0;
        foreach (string line in Unfold(text))
        {
            (string name, Dictionary<string, string> parameters, string value) = Split(line);
            if (name == "BEGIN" && value.Equals("VEVENT", StringComparison.OrdinalIgnoreCase))
                current = new ICalEvent();
            else if (name == "BEGIN" && current is not null)
                nested++;
            else if (name == "END" && current is not null && nested > 0)
                nested--;
            else if (name == "END" && value.Equals("VEVENT", StringComparison.OrdinalIgnoreCase) && current is not null)
            {
                if (current.Uid.Length > 0 && current.Start is not null)
                    events.Add(current);
                current = null;
            }
            else if (current is not null && nested == 0)
                TakeIfReadable(current, name, parameters, value);
        }
        return events;
    }

    public static IReadOnlyList<CopiedEvent> Expand(IReadOnlyList<ICalEvent> events, DateOnly from, DateOnly to)
    {
        Dictionary<(string Uid, string Key), ICalEvent> overrides = events.Where(each => each.RecurrenceId is not null)
            .GroupBy(each => (each.Uid, each.RecurrenceId!.Key)).ToDictionary(group => group.Key, group => group.Last());
        List<CopiedEvent> copies = [];
        foreach (ICalEvent master in events.Where(each => each.RecurrenceId is null).GroupBy(each => each.Uid).Select(group => group.Last()))
        {
            bool repeats = master.Rule is not null || master.RDates.Count > 0;
            HashSet<string> skipped = master.ExDates.Select(time => time.Key).ToHashSet();
            foreach (ICalTime start in StartsOf(master, to))
            {
                if (skipped.Contains(start.Key))
                    continue;
                ICalEvent source = overrides.GetValueOrDefault((master.Uid, start.Key)) ?? master;
                if (source.Cancelled)
                    continue;
                ICalTime actualStart = ReferenceEquals(source, master) ? start : source.Start ?? start;
                string uid = repeats ? $"{master.Uid}/{start.Key}" : master.Uid;
                if (Copy(uid, source, actualStart) is { } copy && Overlaps(copy, from, to))
                    copies.Add(copy);
            }
        }
        return copies;
    }

    private static CopiedEvent? Copy(string uid, ICalEvent source, ICalTime start)
    {
        TimeSpan length = source.End is { } end && source.Start is { } ownStart
            ? end.ToChurch() - ownStart.ToChurch()
            : source.Duration ?? (start.IsDate ? TimeSpan.FromDays(1) : TimeSpan.Zero);
        if (length < TimeSpan.Zero)
            return null;
        DateTime churchStart = start.ToChurch();
        return new CopiedEvent(uid, source, churchStart, churchStart + length, start.IsDate);
    }

    private static bool Overlaps(CopiedEvent copy, DateOnly from, DateOnly to)
    {
        DateOnly first = DateOnly.FromDateTime(copy.Start);
        DateOnly last = DateOnly.FromDateTime(copy.AllDay && copy.End > copy.Start ? copy.End.AddDays(-1) : copy.End);
        return last >= from && first <= to;
    }

    private static IEnumerable<ICalTime> StartsOf(ICalEvent master, DateOnly to)
    {
        ICalTime start = master.Start!;
        IEnumerable<ICalTime> starts = master.Rule is { } rule ? Recur(start, rule, to) : [start];
        return starts.Concat(master.RDates).DistinctBy(time => time.Key);
    }

    private static IEnumerable<ICalTime> Recur(ICalTime start, ICalRule rule, DateOnly to)
    {
        DateTime first = start.Value;
        DateTime last = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
        int produced = 0;
        for (int period = 0; period < MostPeriods; period++)
        {
            List<DateTime> candidates = Candidates(first, rule, period);
            if (candidates.Count == 0 && PeriodStart(first, rule, period) > last)
                yield break;
            foreach (DateTime candidate in candidates.Where(each => each >= first).Order())
            {
                ICalTime time = start.At(candidate);
                if (rule.Until is { } until && Later(time, until))
                    yield break;
                if (rule.Count is { } count && produced >= count)
                    yield break;
                if (candidate > last || produced >= MostInstances)
                    yield break;
                produced++;
                yield return time;
            }
        }
    }

    private static bool Later(ICalTime time, ICalTime until) =>
        until.IsDate ? DateOnly.FromDateTime(time.IsDate ? time.Value : time.ToChurch()) > DateOnly.FromDateTime(until.Value) : time.ToChurch() > until.ToChurch();

    private static DateTime PeriodStart(DateTime first, ICalRule rule, int period) => rule.Frequency switch
    {
        "DAILY" => first.Date.AddDays(period * rule.Interval),
        "WEEKLY" => first.Date.AddDays(-(((int)first.DayOfWeek + 6) % 7)).AddDays(7 * period * rule.Interval),
        "MONTHLY" => new DateTime(first.Year, first.Month, 1).AddMonths(period * rule.Interval),
        "YEARLY" => new DateTime(first.Year, 1, 1).AddYears(period * rule.Interval),
        _ => DateTime.MaxValue,
    };

    private static List<DateTime> Candidates(DateTime first, ICalRule rule, int period)
    {
        DateTime periodStart = PeriodStart(first, rule, period);
        if (periodStart == DateTime.MaxValue)
            return period == 0 ? [first] : [];
        TimeSpan timeOfDay = first.TimeOfDay;
        IEnumerable<DateTime> days = rule.Frequency switch
        {
            "DAILY" => [periodStart],
            "WEEKLY" => Enumerable.Range(0, 7).Select(offset => periodStart.AddDays(offset))
                .Where(day => rule.ByDay.Count == 0 ? day.DayOfWeek == first.DayOfWeek : rule.ByDay.Any(by => by.Day == day.DayOfWeek)),
            "MONTHLY" => DaysInMonth(periodStart, first, rule),
            _ => (rule.ByMonth.Count > 0 ? rule.ByMonth : [first.Month])
                .Where(month => month is >= 1 and <= 12)
                .SelectMany(month => DaysInMonth(new DateTime(periodStart.Year, month, 1), first, rule, yearly: true)),
        };
        return days
            .Where(day => rule.ByMonth.Count == 0 || rule.ByMonth.Contains(day.Month))
            .Where(day => rule.Frequency is "WEEKLY" || rule.ByDay.Count == 0 || rule.ByDay.Any(by => by.Day == day.DayOfWeek))
            .Select(day => day.Date + timeOfDay)
            .ToList();
    }

    private static IEnumerable<DateTime> DaysInMonth(DateTime month, DateTime first, ICalRule rule, bool yearly = false)
    {
        int length = DateTime.DaysInMonth(month.Year, month.Month);
        if (rule.ByMonthDay.Count > 0)
            return rule.ByMonthDay.Select(day => day > 0 ? day : length + day + 1).Where(day => day >= 1 && day <= length).Select(day => month.AddDays(day - 1));
        if (rule.ByDay.Count > 0)
            return rule.ByDay.SelectMany(by => Weekdays(month, length, by.Day, by.Ordinal));
        if (yearly && rule.ByMonth.Count == 0 && first.Month != month.Month)
            return [];
        return first.Day <= length ? [month.AddDays(first.Day - 1)] : [];
    }

    private static IEnumerable<DateTime> Weekdays(DateTime month, int length, DayOfWeek day, int ordinal)
    {
        List<DateTime> all = Enumerable.Range(0, length).Select(offset => month.AddDays(offset)).Where(each => each.DayOfWeek == day).ToList();
        if (ordinal == 0)
            return all;
        int index = ordinal > 0 ? ordinal - 1 : all.Count + ordinal;
        return index >= 0 && index < all.Count ? [all[index]] : [];
    }

    private static void TakeIfReadable(ICalEvent current, string name, Dictionary<string, string> parameters, string value)
    {
        try
        {
            Take(current, name, parameters, value);
        }
        catch (FormatException)
        {
        }
    }

    private static void Take(ICalEvent current, string name, Dictionary<string, string> parameters, string value)
    {
        switch (name)
        {
            case "UID":
                current.Uid = value.Trim();
                break;
            case "SUMMARY":
                current.Summary = Unescape(value).Trim();
                break;
            case "DESCRIPTION":
                current.Description = Unescape(value).Trim();
                break;
            case "LOCATION":
                current.Location = Unescape(value).Trim();
                break;
            case "URL":
                current.Url = value.Trim();
                break;
            case "STATUS":
                current.Cancelled = value.Trim().Equals("CANCELLED", StringComparison.OrdinalIgnoreCase);
                break;
            case "DTSTART":
                current.Start = Time(parameters, value);
                break;
            case "DTEND":
                current.End = Time(parameters, value);
                break;
            case "DURATION":
                current.Duration = Duration(value);
                break;
            case "RRULE":
                current.Rule = Rule(value, current.Start);
                break;
            case "RDATE":
                current.RDates.AddRange(Times(parameters, value));
                break;
            case "EXDATE":
                current.ExDates.AddRange(Times(parameters, value));
                break;
            case "RECURRENCE-ID":
                current.RecurrenceId = Time(parameters, value);
                break;
        }
    }

    private static IEnumerable<ICalTime> Times(Dictionary<string, string> parameters, string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !part.Contains('/'))
            .Select(part => Time(parameters, part));

    private static ICalTime Time(Dictionary<string, string> parameters, string value)
    {
        value = value.Trim();
        if (parameters.GetValueOrDefault("VALUE")?.Equals("DATE", StringComparison.OrdinalIgnoreCase) == true || value.Length == 8)
            return new ICalTime(DateTime.ParseExact(value[..8], "yyyyMMdd", CultureInfo.InvariantCulture), true, null);
        bool utc = value.EndsWith('Z');
        DateTime parsed = DateTime.ParseExact(value.TrimEnd('Z'), "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
        TimeZoneInfo? zone = utc ? TimeZoneInfo.Utc : parameters.TryGetValue("TZID", out string? id) ? Zone(id) : null;
        return new ICalTime(parsed, false, zone);
    }

    private static TimeZoneInfo? Zone(string id) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(id.Trim('"').TrimStart('/'), out TimeZoneInfo? zone) ? zone : null;

    private static ICalRule Rule(string value, ICalTime? start)
    {
        Dictionary<string, string> parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(pair => pair.Length == 2)
            .ToDictionary(pair => pair[0].Trim().ToUpperInvariant(), pair => pair[1].Trim().ToUpperInvariant());
        ICalTime? until = parts.TryGetValue("UNTIL", out string? untilText)
            ? Time(untilText.Length == 8 ? new Dictionary<string, string> { ["VALUE"] = "DATE" } : start?.Zone is { } zone && !untilText.EndsWith('Z') ? new Dictionary<string, string> { ["TZID"] = zone.Id } : [], untilText)
            : null;
        return new ICalRule(
            parts.GetValueOrDefault("FREQ", ""),
            int.TryParse(parts.GetValueOrDefault("INTERVAL"), out int interval) && interval > 0 ? interval : 1,
            int.TryParse(parts.GetValueOrDefault("COUNT"), out int count) && count > 0 ? count : null,
            until,
            List(parts, "BYDAY").Select(Weekday).OfType<(int, DayOfWeek)>().ToList(),
            List(parts, "BYMONTHDAY").Select(part => int.TryParse(part, out int day) ? day : 0).Where(day => day != 0).ToList(),
            List(parts, "BYMONTH").Select(part => int.TryParse(part, out int month) ? month : 0).Where(month => month != 0).ToList());
    }

    private static string[] List(Dictionary<string, string> parts, string key) =>
        parts.TryGetValue(key, out string? text) ? text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : [];

    private static (int, DayOfWeek)? Weekday(string text)
    {
        if (text.Length < 2)
            return null;
        DayOfWeek? day = text[^2..] switch
        {
            "SU" => DayOfWeek.Sunday,
            "MO" => DayOfWeek.Monday,
            "TU" => DayOfWeek.Tuesday,
            "WE" => DayOfWeek.Wednesday,
            "TH" => DayOfWeek.Thursday,
            "FR" => DayOfWeek.Friday,
            "SA" => DayOfWeek.Saturday,
            _ => null,
        };
        if (day is null)
            return null;
        string ordinal = text[..^2];
        return ordinal.Length == 0 ? (0, day.Value) : int.TryParse(ordinal, out int nth) ? (nth, day.Value) : null;
    }

    private static TimeSpan? Duration(string value)
    {
        try
        {
            return System.Xml.XmlConvert.ToTimeSpan(value.Trim());
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static (string Name, Dictionary<string, string> Parameters, string Value) Split(string line)
    {
        int colon = -1;
        bool quoted = false;
        for (int index = 0; index < line.Length && colon < 0; index++)
        {
            if (line[index] == '"')
                quoted = !quoted;
            else if (line[index] == ':' && !quoted)
                colon = index;
        }
        if (colon < 0)
            return ("", [], "");

        string[] head = line[..colon].Split(';');
        Dictionary<string, string> parameters = new(StringComparer.OrdinalIgnoreCase);
        foreach (string parameter in head.Skip(1))
        {
            string[] pair = parameter.Split('=', 2);
            if (pair.Length == 2)
                parameters[pair[0].Trim()] = pair[1].Trim().Trim('"');
        }
        return (head[0].Trim().ToUpperInvariant(), parameters, line[(colon + 1)..]);
    }

    private static IEnumerable<string> Unfold(string text)
    {
        StringBuilder current = new();
        foreach (string raw in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (raw.Length > 0 && (raw[0] == ' ' || raw[0] == '\t'))
            {
                current.Append(raw.AsSpan(1));
                continue;
            }
            if (current.Length > 0)
                yield return current.ToString();
            current.Clear().Append(raw.TrimEnd('\r'));
        }
        if (current.Length > 0)
            yield return current.ToString();
    }

    private static string Unescape(string value)
    {
        StringBuilder text = new(value.Length);
        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] != '\\' || index == value.Length - 1)
            {
                text.Append(value[index]);
                continue;
            }
            char next = value[++index];
            text.Append(next is 'n' or 'N' ? '\n' : next);
        }
        return text.ToString();
    }
}
