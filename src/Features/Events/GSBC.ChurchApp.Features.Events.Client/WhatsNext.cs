using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed record WhatsNextWeek(string Heading, string Range, IReadOnlyList<DigestShownItem> Items);

public sealed record EarlierDigest(string Start, string Range, int Count);

public sealed record WhatsNextDigest(DigestShown? Header, IReadOnlyList<DigestShownItem> Items, IReadOnlyList<EarlierDigest>? Earlier = null)
{
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;
    private static readonly string[] Numbers = ["No", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve"];

    public static readonly WhatsNextDigest None = new(null, []);

    public IReadOnlyList<EarlierDigest> Before => Earlier ?? [];

    public IReadOnlyList<DigestShownItem> Coming(DateOnly today) => StillToCome(today);

    public string SentLead
    {
        get
        {
            string things = Items.Count < Numbers.Length ? $"{Numbers[Items.Count]} {(Items.Count == 1 ? "thing" : "things")}" : $"{Items.Count} things";
            return Header is { } header ? $"{things} coming up, as the church office sent it on {ChurchTime.ToChurch(header.SentAt).ToString("dddd d MMMM", English)}." : "";
        }
    }

    public IReadOnlyList<WhatsNextWeek> WeeksAsSent() => Items
        .GroupBy(item => ChurchTime.WeekStart(FirstOf(item) ?? DateOnly.MinValue))
        .OrderBy(week => week.Key)
        .Select(week => new WhatsNextWeek($"Week of {week.Key.ToString("d MMMM", English)}", RangeOf(week.Key), week.ToList()))
        .ToList();

    private IReadOnlyList<DigestShownItem> StillToCome(DateOnly today) =>
        Items.Where(item => (LastOf(item) ?? DateOnly.MaxValue) >= today).ToList();

    private DateOnly? FirstOf(DigestShownItem item) => EventDates.Parse(item.FirstDate) ?? EventDates.Parse(Header?.Start ?? "");

    private DateOnly? LastOf(DigestShownItem item) =>
        EventDates.Parse(item.LastDate) ?? EventDates.Parse(item.FirstDate) ?? EventDates.Parse(Header?.End ?? "");

    public string Range => Header is { } header && EventDates.Parse(header.Start) is { } start && EventDates.Parse(header.End) is { } end ? EventWords.Range(start, end) : "";

    public string Lead(DateOnly today)
    {
        int count = StillToCome(today).Count;
        string things = count < Numbers.Length ? $"{Numbers[count]} {(count == 1 ? "thing" : "things")}" : $"{count} things";
        int weeks = Header is { } header && EventDates.Parse(header.Start) is { } start && EventDates.Parse(header.End) is { } end
            ? Math.Max(1, (end.DayNumber - Math.Max(start.DayNumber, today.DayNumber) + 7) / 7)
            : 1;
        string ahead = weeks switch { 1 => "the week ahead", 2 => "the next two weeks", _ => $"the next {Numbers[Math.Min(weeks, Numbers.Length - 1)].ToLowerInvariant()} weeks" };
        return $"{things} coming up at Good Shepherd in {ahead}.";
    }

    public IReadOnlyList<WhatsNextWeek> Weeks(DateOnly today)
    {
        DateOnly thisWeek = ChurchTime.WeekStart(Header is { } header && EventDates.Parse(header.Start) is { } start && today < start ? start : today);
        return StillToCome(today)
            .GroupBy(item => ChurchTime.WeekStart(Max(FirstOf(item) ?? today, today)))
            .OrderBy(week => week.Key)
            .Select(week => new WhatsNextWeek(Heading(week.Key, thisWeek), RangeOf(week.Key), week.ToList()))
            .ToList();
    }

    public static string WhenAsSent(DigestShownItem item)
    {
        if (EventDates.Parse(item.FirstDate) is not { } first)
            return WhenLine(item.Words);
        DateOnly last = EventDates.Parse(item.LastDate) is { } end && end > first ? end : first;
        string days = last == first ? EventWords.LongDate(first) : EventWords.Range(first, last);
        return item.Words.Time.Length > 0 ? $"{days} · {item.Words.Time}" : days;
    }

    public static string WhenLine(DigestWords words, string between = " · ") =>
        words.Time.Length > 0 && words.When.Length > 0 ? $"{words.When}{between}{words.Time}" : words.When + words.Time;

    private static string Heading(DateOnly week, DateOnly thisWeek) => (week.DayNumber - thisWeek.DayNumber) switch
    {
        <= 0 => "This week",
        7 => "Next week",
        _ => $"Week of {week.ToString("d MMMM", English)}",
    };

    private static string RangeOf(DateOnly week) => Dates(week, week.AddDays(6));

    public static string Dates(DateOnly first, DateOnly last) => first.Month == last.Month
        ? $"{first.Day} – {last.ToString("d MMMM", English)}"
        : $"{first.ToString("d MMMM", English)} – {last.ToString("d MMMM", English)}";

    private static DateOnly Max(DateOnly first, DateOnly second) => first > second ? first : second;
}

public sealed class DigestReader(IServiceProvider services)
{
    private const int Page = 200;
    private readonly ILocalFeeds? _feeds = services.GetService(typeof(ILocalFeeds)) as ILocalFeeds;

    public static readonly IReadOnlyList<string> Feeds = [EventsFeeds.Digest, EventsFeeds.PrivateDigest];

    public const string Remembered = "events.whats-next";

    public Task<WhatsNextDigest> LoadAsync() => LoadAsync(null);

    public async Task<WhatsNextDigest> LoadAsync(string? start)
    {
        if (_feeds is not { IsAvailable: true } feeds)
            return WhatsNextDigest.None;
        List<StoredRecord> records = [.. await feeds.ReadPageAsync(new FeedPageQuery(EventsFeeds.Digest, Limit: Page)),
            .. await feeds.ReadPageAsync(new FeedPageQuery(EventsFeeds.PrivateDigest, Limit: Page))];
        List<StoredRecord> headers = records.Where(record => record.Id == HeaderId || record.Id.StartsWith(EarlierPrefix, StringComparison.Ordinal)).ToList();
        if (headers.FirstOrDefault(record => record.Id == HeaderId) is null)
            return WhatsNextDigest.None;
        List<DigestShown> shown = headers.Select(Decode<DigestShown>).OrderByDescending(header => header.Start, StringComparer.Ordinal).ToList();
        DigestShown? chosen = start is null ? Decode<DigestShown>(headers.First(record => record.Id == HeaderId)) : shown.FirstOrDefault(header => header.Start == start);
        if (chosen is null)
            return WhatsNextDigest.None;
        List<EarlierDigest> earlier = shown.Where(header => string.CompareOrdinal(header.Start, chosen.Start) < 0)
            .Select(header => new EarlierDigest(header.Start, RangeOf(header), ItemsOf(records, header).Count)).Where(digest => digest.Count > 0).ToList();
        return new WhatsNextDigest(chosen, ItemsOf(records, chosen), earlier);
    }

    private static List<DigestShownItem> ItemsOf(IEnumerable<StoredRecord> records, DigestShown header)
    {
        string prefix = $"{header.Start}/";
        return records.Where(record => record.Id.StartsWith(prefix, StringComparison.Ordinal)).Select(Decode<DigestShownItem>).OrderBy(item => item.Order).ToList();
    }

    private static string RangeOf(DigestShown header) =>
        EventDates.Parse(header.Start) is { } start && EventDates.Parse(header.End) is { } end ? WhatsNextDigest.Dates(start, end) : "";

    private const string HeaderId = "digest";
    private const string EarlierPrefix = $"{HeaderId}:";

    private static T Decode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(StoredRecord record) =>
        EventsContractsModel.Instance.Deserialize<T>(new MemoryStream(record.Payload));
}
