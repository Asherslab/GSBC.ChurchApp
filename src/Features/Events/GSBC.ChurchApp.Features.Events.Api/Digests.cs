using System.Globalization;
using System.Text.RegularExpressions;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Api;

internal static partial class Digests
{
    public const int DefaultWeeks = 2;
    public const int MostWeeks = 3;
    public const string OwnPrefix = "own:";

    private static readonly CultureInfo English = CultureInfo.InvariantCulture;
    private static readonly string[] Counted = ["", "one", "two", "three", "four", "five"];

    public static DateOnly NextStart(DateOnly today)
    {
        int days = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(days == 0 ? 7 : days);
    }

    public static DateOnly EndOf(DateOnly start, int weeks) => start.AddDays(7 * weeks - 1);

    public static IReadOnlySet<Guid> DefaultCalendars(EventsBook book) =>
        book.Calendars.Values.Where(calendar => calendar.ICalSource is not { Length: > 0 }).Select(calendar => calendar.Id).ToHashSet();

    private static bool Worth(EventOccurrence occurrence) =>
        occurrence.Kind != OccurrenceKind.Regular || occurrence.NotOn || occurrence.Missing.Count > 0;

    public static List<DigestItem> Candidates(EventsBook book, DateOnly start, int weeks, IReadOnlySet<Guid> calendars, DateTime now)
    {
        DateOnly end = EndOf(start, weeks);
        List<EventOccurrence> occurrences = book
            .Occurrences(start, end, item => EventsBook.IsShown(item, now) && (calendars.Contains(item.CalendarId) || book.AlsoOn(item.Id).Any(calendars.Contains)))
            .Where(Worth)
            .OrderBy(occurrence => occurrence.StartsAt).ThenBy(occurrence => occurrence.Fields.Title)
            .ToList();

        return occurrences
            .GroupBy(GroupOf)
            .Select(group => ItemOf(book, group.ToList(), start))
            .ToList();
    }

    public static List<DigestItem> Merge(IReadOnlyList<DigestItem> candidates, IReadOnlyList<DigestEntry> entries)
    {
        Dictionary<string, DigestItem> waiting = candidates.ToDictionary(candidate => candidate.Key);
        List<DigestItem> merged = [];
        foreach (DigestEntry entry in entries)
        {
            if (entry.Own)
                merged.Add(OwnItem(entry));
            else if (waiting.Remove(entry.Key, out DigestItem? item))
            {
                item.Included = entry.Included;
                item.Changed = entry.Changed.Where(DigestFields.All.Contains).Distinct().ToList();
                item.Words = entry.Words;
                merged.Add(item);
            }
        }

        foreach (DigestItem fresh in candidates.Where(candidate => waiting.ContainsKey(candidate.Key)))
        {
            fresh.IsNew = entries.Count > 0;
            int before = merged.FindLastIndex(item => item.Kind != DigestItemKind.Own && string.CompareOrdinal(item.FirstDate, fresh.FirstDate) <= 0);
            merged.Insert(before + 1, fresh);
        }
        return merged;
    }

    public static List<DigestEntry> Kept(IReadOnlyList<DigestEntry> before, IReadOnlyList<DigestEntry> saved, IReadOnlySet<string> showing)
    {
        HashSet<string> savedKeys = saved.Select(entry => entry.Key).ToHashSet();
        return [.. saved, .. before.Where(entry => !entry.Own && !savedKeys.Contains(entry.Key) && !showing.Contains(entry.Key))];
    }

    public static SentDigest ForApp(IReadOnlyList<DigestItem> items, DateOnly end) => new()
    {
        End = end,
        Items = items.Where(item => item.Included).Select((item, order) => new SentDigestItem
        {
            Item = new DigestShownItem
            {
                Key = item.Key,
                Order = order,
                Kind = item.Kind,
                Words = item.Shown,
                Picture = item.Picture,
                Place = item.Place,
                Contacts = item.Contacts,
                OccurrenceId = item.OccurrenceIds.FirstOrDefault() ?? "",
                FirstDate = item.FirstDate,
                LastDate = item.LastDate,
            },
            CalendarIds = item.CalendarIds.Select(id => Guid.TryParse(id, out Guid calendarId) ? calendarId : Guid.Empty).Where(id => id != Guid.Empty).ToList(),
        }).ToList(),
    };

    public static IEnumerable<SentDigestItem> Standing(EventsBook book, SentDigest sent) =>
        sent.Items.Where(item => item.CalendarIds.Count == 0 || book.Calendars.ContainsKey(item.CalendarIds[0]));

    public static Audience AudienceOf(EventsBook book, SentDigestItem sent) =>
        sent.CalendarIds.Count == 0
            ? Audience.Everyone
            : CalendarAudiences.Union(sent.CalendarIds.Select(id => book.Calendars.GetValueOrDefault(id)).OfType<CalendarRecord>().Select(calendar => CalendarAudiences.Of(calendar.Audience)));

    public static Notification Notification(EventsBook book, SentDigest sent, NotificationAudience audience)
    {
        List<string> titles = Standing(book, sent).Where(item => AudienceOf(book, item) == Audience.Everyone)
            .Select(item => item.Item.Words.Title).Where(title => title.Length > 0).ToList();
        string things = titles.Count == 1 ? "One thing" : $"{titles.Count} things";
        string named = Listed(titles.Count > 2 ? [.. titles.Take(2), "more"] : titles);
        return new Notification(EventsNotifications.DigestOut, audience, "What's next at Good Shepherd", $"{things} coming up: {named}.",
            Link: EventsDestinations.WhatsNext.Path, Action: "Open");
    }

    public static string When(IReadOnlyList<DateOnly> dates, DateOnly start)
    {
        DateOnly firstWeekEnds = start.AddDays(6);
        DateOnly first = dates[0];
        if (dates.Count == 1)
            return first <= firstWeekEnds ? $"This {LongDay(first)}" : LongDay(first);

        bool weekly = dates.Zip(dates.Skip(1)).All(pair => pair.Second.DayNumber - pair.First.DayNumber == 7);
        string weekday = first.ToString("dddd", English);
        if (weekly && first <= firstWeekEnds && dates.Count < Counted.Length)
            return $"The next {Counted[dates.Count]} {weekday}s";
        if (weekly)
            return $"{weekday}s {DaysAndMonths(dates, day => day.ToString("%d", English))}";
        return DaysAndMonths(dates, day => day.ToString("dddd d", English));
    }

    public static string Span(DateOnly first, DateOnly last, DateOnly start)
    {
        if (first < start)
            return $"Until {LongDay(last)}";
        string from = first.Month == last.Month ? first.ToString("dddd d", English) : LongDay(first);
        string span = $"{from} to {LongDay(last)}";
        return first <= start.AddDays(6) ? $"This {span}" : span;
    }

    private static string PlainText(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return "";
        string text = MarkdownLink().Replace(markdown, "$1");
        text = MarkdownEmphasis().Replace(text, "");
        text = MarkdownLineStart().Replace(text, "");
        return string.Join("\n\n", BlankLines().Split(text.Trim()).Select(paragraph => Spaces().Replace(paragraph, " ").Trim()).Where(paragraph => paragraph.Length > 0));
    }

    private static string GroupOf(EventOccurrence occurrence) => occurrence switch
    {
        { Kind: OccurrenceKind.OneOff, NotOn: false } => $"event:{occurrence.ItemId}",
        { NotOn: true } => $"off:{occurrence.ItemId}:{occurrence.NotOnReason}",
        _ => $"date:{occurrence.ItemId}:{occurrence.Fields.Title}:{PlaceOf(occurrence)}:{TimeOf(occurrence)}:{string.Join(',', occurrence.Missing)}",
    };

    private static DigestItem ItemOf(EventsBook book, IReadOnlyList<EventOccurrence> occurrences, DateOnly start)
    {
        EventOccurrence first = occurrences[0];
        List<DateOnly> dates = occurrences.Select(DayOf).ToList();
        CalendarRecord? calendar = Guid.TryParse(first.CalendarId, out Guid calendarId) ? book.Calendars.GetValueOrDefault(calendarId) : null;
        string label = first.Fields.Label.Length > 0 ? first.Fields.Label : calendar?.Name ?? "";
        DateOnly last = first.Kind == OccurrenceKind.OneOff && EventDates.Parse(first.EndDate) is { } endDate ? endDate : dates[^1];
        DigestItem item = new()
        {
            Kind = first.NotOn ? DigestItemKind.NotOn : DigestItemKind.Event,
            Included = true,
            OccurrenceIds = occurrences.Select(occurrence => occurrence.Id).ToList(),
            FirstDate = EventDates.Format(dates[0]),
            LastDate = EventDates.Format(last),
            CalendarId = first.CalendarId,
            CalendarName = calendar?.Name ?? "",
            CalendarIds = [first.CalendarId, .. first.AlsoOn],
            ForEveryone = book.AudienceOf(first) == Audience.Everyone,
        };

        if (first.NotOn)
        {
            item.Key = $"off:{first.ItemId}:{item.FirstDate}";
            bool aBreak = first.NotOnReason.Length > 0 && Guid.TryParse(first.ItemId, out Guid regularId)
                && book.BreaksOf(regularId).Any(stop => stop.Name == first.NotOnReason && stop.StartDate <= dates[0] && dates[0] <= stop.EndDate);
            item.CalendarWords = new DigestWords
            {
                Label = aBreak ? $"{label} on break" : "Not on",
                Title = $"No {first.Fields.Title}",
                When = When(dates, start),
                Description = OffWords(first),
            };
            return item;
        }

        item.Key = first.Kind == OccurrenceKind.OneOff ? $"event:{first.ItemId}" : $"date:{first.ItemId}:{item.FirstDate}";
        item.Picture = first.Fields.Picture;
        item.Place = PlaceOf(first);
        item.Contacts = string.Join(" and ", first.Fields.Contacts.Select(contact => contact.Name).Where(name => name.Length > 0));
        item.Missing = first.Missing;
        item.CalendarWords = new DigestWords
        {
            Label = label,
            Title = first.Fields.Title,
            When = first.Kind == OccurrenceKind.OneOff && last != dates[0] ? Span(dates[0], last, start) : When(dates, start),
            Time = TimeOf(first),
            Description = string.Join(" ", new[] { PlainText(first.Fields.Description?.Markdown) }.Concat(first.Missing.Select(need => $"{EventFieldKeys.Missing(need)}."))
                .Where(part => part.Length > 0)),
            RegisterLink = first.Fields.RegisterLink,
        };
        return item;
    }

    private static DigestItem OwnItem(DigestEntry entry) => new()
    {
        Key = entry.Key,
        Kind = DigestItemKind.Own,
        Included = entry.Included,
        Words = entry.Words,
        Changed = [.. DigestFields.All],
        ForEveryone = true,
    };

    private static string OffWords(EventOccurrence occurrence)
    {
        string title = occurrence.Fields.Title;
        string? back = EventDates.Parse(occurrence.BackOn) is { } day ? LongDay(day) : null;
        return (occurrence.NotOnReason.Length > 0, back) switch
        {
            (true, { } on) => $"{title} is off for the {occurrence.NotOnReason}, and back on {on}.",
            (true, null) => $"{title} is off for the {occurrence.NotOnReason}.",
            (false, { } on) => $"{title} isn't on, and is back on {on}.",
            _ => $"{title} isn't on.",
        };
    }

    private static string PlaceOf(EventOccurrence occurrence) => occurrence.Fields.Place switch
    {
        { Name.Length: > 0 } place => place.Name,
        { Address.Length: > 0 } place => place.Address,
        _ => "",
    };

    private static string TimeOf(EventOccurrence occurrence) =>
        occurrence.AllDay ? "" : EventDates.Time((int)ChurchTime.ToChurch(occurrence.StartsAt).TimeOfDay.TotalMinutes);

    private static DateOnly DayOf(EventOccurrence occurrence) => EventDates.Parse(occurrence.Date) ?? ChurchTime.DayOf(occurrence.StartsAt);

    private static string LongDay(DateOnly day) => day.ToString("dddd d MMMM", English);

    private static string DaysAndMonths(IReadOnlyList<DateOnly> dates, Func<DateOnly, string> day)
    {
        List<string> parts = dates.GroupBy(date => (date.Year, date.Month))
            .Select(month => $"{Listed(month.Select(day).ToList())} {month.First().ToString("MMMM", English)}")
            .ToList();
        return Listed(parts);
    }

    private static string Listed(IReadOnlyList<string> parts) =>
        parts.Count <= 1 ? string.Concat(parts) : $"{string.Join(", ", parts.Take(parts.Count - 1))} and {parts[^1]}";

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"(\*\*|__|\*|`)")]
    private static partial Regex MarkdownEmphasis();

    [GeneratedRegex(@"(?m)^\s*(#{1,6}\s+|>\s?|[-+]\s+)")]
    private static partial Regex MarkdownLineStart();

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex BlankLines();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
