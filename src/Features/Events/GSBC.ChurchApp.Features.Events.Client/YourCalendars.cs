using System.Globalization;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Client.Signals;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed class YourCalendars(ChannelBase channel, ILocalPreferences preferences, ClientSignals signals) : IClientStartupTask, IDisposable
{
    private const string AddedKey = "events.added";
    private const string TurnedOffKey = "events.turned-off";
    private const string SeenKey = "events.seen";
    private const int SeenKept = 200;
    private readonly IEventsService _events = channel.CreateGrpcService<IEventsService>();
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
        _mine = new MyCalendars { Added = await ListAsync(AddedKey), TurnedOff = await ListAsync(TurnedOffKey) };
        _seen = await ListAsync(SeenKey);
        signals.Changed += OnSignals;
        _ = RefreshAsync();
    }

    public async Task<string?> ChooseAsync(string calendarId, bool inYourWeek)
    {
        try
        {
            await TakeAsync(await _events.ChooseAsync(new CalendarChoice { CalendarId = calendarId, InYourWeek = inYourWeek }));
            if (inYourWeek)
                Hide(calendarId, false);
            return null;
        }
        catch (RpcException exception) when (exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            return "That needs a connection. Try again when you're online.";
        }
        catch (RpcException exception)
        {
            return exception.Status.Detail;
        }
    }

    public void Dispose() => signals.Changed -= OnSignals;

    private static string SeenMark(string occurrenceId, DateTime changedAt) =>
        $"{occurrenceId}@{changedAt.Ticks.ToString(CultureInfo.InvariantCulture)}";

    private async Task<List<string>> ListAsync(string key) =>
        (await preferences.GetAsync(key) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

    private async Task RefreshAsync()
    {
        try
        {
            await TakeAsync(await _events.MineAsync(new Done()));
        }
        catch (RpcException)
        {
        }
    }

    private async Task TakeAsync(MyCalendars mine)
    {
        _mine = mine;
        await preferences.SetAsync(AddedKey, string.Join(',', mine.Added));
        await preferences.SetAsync(TurnedOffKey, string.Join(',', mine.TurnedOff));
        Changed?.Invoke();
    }

    private void OnSignals(ClientChange change)
    {
        if (change.Concerns(ChangeKind.Session | ChangeKind.Reconnected | ChangeKind.Access, null))
            _ = RefreshAsync();
    }
}
