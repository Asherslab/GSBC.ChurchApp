using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed record WhatsNextWeek(string Heading, string Range, IReadOnlyList<DigestShownItem> Items);

public sealed record WhatsNextDigest(DigestShown? Header, IReadOnlyList<DigestShownItem> Items)
{
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;
    private static readonly string[] Numbers = ["No", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve"];

    public static readonly WhatsNextDigest None = new(null, []);

    private IReadOnlyList<DigestShownItem> StillToCome(DateOnly today) =>
        Items.Where(item => (EventDates.Parse(item.LastDate) ?? EventDates.Parse(item.FirstDate) ?? DateOnly.MaxValue) >= today).ToList();

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
        DateOnly thisWeek = Header is { } header && EventDates.Parse(header.Start) is { } start && today < start ? start : ChurchTime.WeekStart(today);
        return StillToCome(today)
            .GroupBy(item => ChurchTime.WeekStart(Max(EventDates.Parse(item.FirstDate) ?? today, today)))
            .OrderBy(week => week.Key)
            .Select(week => new WhatsNextWeek(Heading(week.Key, thisWeek), RangeOf(week.Key), week.ToList()))
            .ToList();
    }

    public static string WhenLine(DigestWords words, string between = " · ") =>
        words.Time.Length > 0 && words.When.Length > 0 ? $"{words.When}{between}{words.Time}" : words.When + words.Time;

    private static string Heading(DateOnly week, DateOnly thisWeek) => (week.DayNumber - thisWeek.DayNumber) switch
    {
        <= 0 => "This week",
        7 => "Next week",
        _ => $"Week of {week.ToString("d MMMM", English)}",
    };

    private static string RangeOf(DateOnly week)
    {
        DateOnly last = week.AddDays(6);
        return week.Month == last.Month
            ? $"{week.Day} – {last.ToString("d MMMM", English)}"
            : $"{week.ToString("d MMMM", English)} – {last.ToString("d MMMM", English)}";
    }

    private static DateOnly Max(DateOnly first, DateOnly second) => first > second ? first : second;
}

public sealed class DigestReader(IServiceProvider services)
{
    private const int Page = 200;
    private readonly ILocalFeeds? _feeds = services.GetService(typeof(ILocalFeeds)) as ILocalFeeds;

    public static readonly IReadOnlyList<string> Feeds = [EventsFeeds.Digest, EventsFeeds.PrivateDigest];

    public const string Remembered = "events.whats-next";

    public async Task<WhatsNextDigest> LoadAsync()
    {
        if (_feeds is not { IsAvailable: true } feeds)
            return WhatsNextDigest.None;
        IReadOnlyList<StoredRecord> shared = await feeds.ReadPageAsync(new FeedPageQuery(EventsFeeds.Digest, Limit: Page));
        IReadOnlyList<StoredRecord> yours = await feeds.ReadPageAsync(new FeedPageQuery(EventsFeeds.PrivateDigest, Limit: Page));
        StoredRecord? header = shared.FirstOrDefault(record => record.Id == HeaderId);
        if (header is null)
            return WhatsNextDigest.None;
        DigestShown shown = Decode<DigestShown>(header);
        string prefix = $"{shown.Start}/";
        return new WhatsNextDigest(shown, shared.Concat(yours).Where(record => record.Id.StartsWith(prefix, StringComparison.Ordinal))
            .Select(Decode<DigestShownItem>).OrderBy(item => item.Order).ToList());
    }

    private const string HeaderId = "digest";

    private static T Decode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(StoredRecord record) =>
        EventsContractsModel.Instance.Deserialize<T>(new MemoryStream(record.Payload));
}
