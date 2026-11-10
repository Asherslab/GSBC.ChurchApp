using System.Globalization;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Client.Signals;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed class YourCalendars(ILocalFeeds feeds, IAccess access, IOutbox outbox, ILocalPreferences preferences, ClientSignals signals)
    : IClientStartupTask, IDisposable
{
    private const string ChooseMethod = "/gsbc.events.Events/Choose";
    private const string AddedKey = "events.added";
    private const string TurnedOffKey = "events.turned-off";
    private const string SeenKey = "events.seen";
    private const int SeenKept = 200;
    private MyCalendars _mine = new();
    private readonly HashSet<string> _hidden = [];
    private List<string> _seen = [];

    public event Action? Changed;

    public MyCalendars Mine => _mine;

    public bool InYourWeek(CalendarInfo calendar) => CalendarChoices.InYourWeek(calendar, _mine);

    public IReadOnlyList<CalendarInfo> InYourWeek(IEnumerable<CalendarInfo> calendars) => calendars.Where(InYourWeek).ToList();

    public bool IsHidden(string calendarId) => _hidden.Contains(calendarId);

    public IReadOnlySet<string> Showing(IEnumerable<CalendarInfo> calendars) =>
        calendars.Where(calendar => InYourWeek(calendar) && !_hidden.Contains(calendar.Id)).Select(calendar => calendar.Id).ToHashSet();

    public void Hide(string calendarId, bool hidden)
    {
        if (hidden ? _hidden.Add(calendarId) : _hidden.Remove(calendarId))
            Changed?.Invoke();
    }

    public bool IsNewChange(EventOccurrence occurrence) =>
        occurrence.ChangedAt is { } at && !_seen.Contains(SeenMark(occurrence.Id, at));

    public async Task SeenAsync(EventOccurrence occurrence)
    {
        if (occurrence.ChangedAt is not { } at || !IsNewChange(occurrence))
            return;
        _seen = [SeenMark(occurrence.Id, at), .. _seen.Take(SeenKept - 1)];
        await preferences.SetAsync(SeenKey, string.Join(',', _seen));
        Changed?.Invoke();
    }

    public async Task RunAsync()
    {
        _seen = await ListAsync(SeenKey);
        signals.Changed += OnSignals;
        await RefreshAsync();
    }

    public async Task<string?> ChooseAsync(string calendarId, bool inYourWeek)
    {
        await outbox.EnqueueAsync(ChooseMethod, new CalendarChoice { CalendarId = calendarId, InYourWeek = inYourWeek }, EventsContractsModel.Instance);
        MyCalendars mine = new()
        {
            Added = [.. _mine.Added.Where(id => id != calendarId), .. inYourWeek ? [calendarId] : Array.Empty<string>()],
            TurnedOff = [.. _mine.TurnedOff.Where(id => id != calendarId), .. inYourWeek ? Array.Empty<string>() : [calendarId]],
        };
        await TakeAsync(mine, remember: !access.SignedIn);
        if (inYourWeek)
            Hide(calendarId, false);
        return null;
    }

    public void Dispose() => signals.Changed -= OnSignals;

    private static string SeenMark(string occurrenceId, DateTime changedAt) =>
        $"{occurrenceId}@{changedAt.Ticks.ToString(CultureInfo.InvariantCulture)}";

    private async Task<List<string>> ListAsync(string key) =>
        (await preferences.GetAsync(key) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

    private async Task RefreshAsync()
    {
        if (!access.SignedIn)
        {
            await TakeAsync(new MyCalendars { Added = await ListAsync(AddedKey), TurnedOff = await ListAsync(TurnedOffKey) }, remember: false);
            return;
        }
        IReadOnlyList<StoredRecord> stored = await feeds.ReadPageAsync(new FeedPageQuery(EventsFeeds.Mine, Limit: 1));
        await TakeAsync(stored.Count == 0 ? new MyCalendars() : EventsContractsModel.Instance.Deserialize<MyCalendars>(new MemoryStream(stored[0].Payload)), remember: false);
    }

    private async Task TakeAsync(MyCalendars mine, bool remember)
    {
        _mine = mine;
        if (remember)
        {
            await preferences.SetAsync(AddedKey, string.Join(',', mine.Added));
            await preferences.SetAsync(TurnedOffKey, string.Join(',', mine.TurnedOff));
        }
        Changed?.Invoke();
    }

    private void OnSignals(ClientChange change)
    {
        if (change.Concerns(ChangeKind.Session | ChangeKind.Feeds, [EventsFeeds.Mine]))
            _ = RefreshAsync();
    }
}
