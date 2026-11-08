using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed record Diary(IReadOnlyList<CalendarInfo> Calendars, IReadOnlyList<EventOccurrence> Occurrences, IReadOnlyDictionary<string, List<EventContact>> ContactDetails)
{
    public static readonly Diary Empty = new([], [], new Dictionary<string, List<EventContact>>());

    public CalendarInfo? Calendar(string id) => Calendars.FirstOrDefault(calendar => calendar.Id == id || calendar.Slug == id);

    public EventOccurrence? Find(string id) => Occurrences.FirstOrDefault(occurrence => occurrence.Id == id);

    public IReadOnlyList<EventOccurrence> In(IReadOnlySet<string> calendarIds) =>
        Occurrences.Where(occurrence => calendarIds.Contains(occurrence.CalendarId) || occurrence.AlsoOn.Any(calendarIds.Contains)).ToList();

    public IReadOnlyList<EventOccurrence> OtherDates(EventOccurrence occurrence, DateTime nowUtc) =>
        occurrence.Kind == OccurrenceKind.OneOff
            ? []
            : Occurrences.Where(other => other.ItemId == occurrence.ItemId && other.Id != occurrence.Id && other.EndsAt >= nowUtc).ToList();

    public IReadOnlyList<EventContact> ContactsOf(EventOccurrence occurrence) =>
        ContactDetails.TryGetValue(occurrence.Id, out List<EventContact>? detailed) ? detailed : occurrence.Fields.Contacts;
}

public sealed class CalendarDiary(IServiceProvider services)
{
    private readonly ILocalFeeds? _feeds = services.GetService(typeof(ILocalFeeds)) as ILocalFeeds;
    private const int Page = 500;

    public static readonly IReadOnlyList<string> Feeds =
        [EventsFeeds.Calendars, EventsFeeds.PrivateCalendars, EventsFeeds.Public, EventsFeeds.Private, EventsFeeds.Contacts];

    public const string Remembered = "events.diary";

    public bool IsOnThisDevice => _feeds?.IsAvailable == true;

    public async Task<Diary> LoadAsync()
    {
        if (_feeds is not { IsAvailable: true } feeds)
            return Diary.Empty;
        List<CalendarInfo> calendars = [.. await ReadAllAsync<CalendarInfo>(feeds, EventsFeeds.Calendars), .. await ReadAllAsync<CalendarInfo>(feeds, EventsFeeds.PrivateCalendars)];
        List<EventOccurrence> occurrences = [.. await ReadAllAsync<EventOccurrence>(feeds, EventsFeeds.Public, FeedOrder.StartsAt), .. await ReadAllAsync<EventOccurrence>(feeds, EventsFeeds.Private, FeedOrder.StartsAt)];
        List<OccurrenceContacts> contacts = await ReadAllAsync<OccurrenceContacts>(feeds, EventsFeeds.Contacts);
        return new Diary(
            calendars.OrderBy(calendar => calendar.Order).ThenBy(calendar => calendar.Name).ToList(),
            occurrences.OrderBy(occurrence => occurrence.StartsAt).ThenBy(occurrence => occurrence.Fields.Title).ToList(),
            contacts.ToDictionary(entry => entry.OccurrenceId, entry => entry.Contacts));
    }

    public static IReadOnlyList<EventOccurrence> ComingUp(IEnumerable<EventOccurrence> occurrences, DateTime nowUtc, int count)
    {
        DateOnly today = ChurchTime.DayOf(nowUtc);
        return occurrences
            .Where(occurrence => occurrence.EndsAt >= nowUtc || (occurrence.NotOn && ChurchTime.DayOf(occurrence.StartsAt) >= today))
            .Where(occurrence => occurrence.Kind != OccurrenceKind.Regular || occurrence.NotOn || occurrence.ChangedAt is not null)
            .Take(count)
            .ToList();
    }

    private static async Task<List<T>> ReadAllAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(ILocalFeeds feeds, string feed, FeedOrder order = FeedOrder.SortKey)
    {
        List<T> all = [];
        for (int offset = 0; ; offset += Page)
        {
            IReadOnlyList<StoredRecord> page = await feeds.ReadPageAsync(new FeedPageQuery(feed, order, Limit: Page, Offset: offset));
            all.AddRange(page.Select(record => EventsContractsModel.Instance.Deserialize<T>(new MemoryStream(record.Payload))));
            if (page.Count < Page)
                return all;
        }
    }
}
