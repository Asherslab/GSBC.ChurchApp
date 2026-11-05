using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Audio;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Client.Signals;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public sealed class Listening : IClientStartupTask, IDisposable
{
    public const string SaveMethod = "/gsbc.sermons.Sermons/SaveProgress";
    public const string Finished = "finished";
    public const string Watched = "watched";
    public const string Cleared = "cleared";
    private const string SentUpTo = "sermons.progress.sent";
    private const int Batch = 200;

    private readonly IAudioPlayer _player;
    private readonly ILocalFeeds _feeds;
    private readonly IOutbox _outbox;
    private readonly IAccess _access;
    private readonly ILocalPreferences _preferences;
    private readonly ClientSignals _signals;
    private readonly NativeBridge? _bridge;
    private readonly SemaphoreSlim _sending = new(1, 1);
    private bool _wasPlaying;
    private bool _wasSignedIn;

    public Listening(IAudioPlayer player, ILocalFeeds feeds, IOutbox outbox, IAccess access, ILocalPreferences preferences, ClientSignals signals, IServiceProvider services)
    {
        _player = player;
        _feeds = feeds;
        _outbox = outbox;
        _access = access;
        _preferences = preferences;
        _signals = signals;
        _bridge = services.GetService(typeof(NativeBridge)) as NativeBridge;
    }

    public static IReadOnlyList<string> Feeds { get; } = [SermonsFeeds.Progress];

    public async Task RunAsync()
    {
        _wasSignedIn = _access.SignedIn;
        _player.Changed += OnPlayerChanged;
        _signals.Changed += OnSignals;
        if (_bridge is not null)
            _bridge.AppStateChanged += OnAppState;
        await _player.StartAsync();
        await SendAsync();
    }

    public async Task<IReadOnlyDictionary<string, ListeningEntry>> LoadAsync()
    {
        Dictionary<string, ListeningEntry> entries = [];
        if (_feeds.IsAvailable && _access.SignedIn)
            foreach (StoredRecord record in await _feeds.ReadPageAsync(new FeedPageQuery(SermonsFeeds.Progress, Descending: true, Limit: 2000)))
            {
                ListeningEntry entry = SermonsContractsModel.Instance.Deserialize<ListeningEntry>(new MemoryStream(record.Payload));
                entries[entry.SermonId] = entry;
            }

        foreach (ListeningEntry local in (await _player.JournalAsync(0)).Records.Select(Entry))
            entries[local.SermonId] = ListeningProgress.MergeEither(entries.GetValueOrDefault(local.SermonId), local)!;

        if (_player.State is { Item: { } item, Position: > 0 } state)
            entries[item.Id] = ListeningProgress.MergeEither(entries.GetValueOrDefault(item.Id),
                ListeningProgress.Heard(item.Id, state.Position, state.Duration, DateTime.UtcNow))!;
        return entries;
    }

    public async Task MarkAsync(string sermonId, string mark)
    {
        await _player.MarkAsync(sermonId, mark, DateTime.UtcNow);
        await SendAsync();
    }

    public async Task SendAsync(bool everything = false)
    {
        if (!_access.SignedIn || !_outbox.IsAvailable)
            return;
        await _sending.WaitAsync();
        try
        {
            long since = everything ? 0 : long.TryParse(await _preferences.GetAsync(SentUpTo), out long sent) ? sent : 0;
            PlaybackJournal journal = await _player.JournalAsync(since);
            foreach (PlaybackRecord[] chunk in journal.Records.Chunk(Batch))
                await _outbox.EnqueueAsync(SaveMethod, new ProgressBatch { Entries = chunk.Select(Entry).ToList() }, SermonsContractsModel.Instance);
            await _preferences.SetAsync(SentUpTo, journal.LatestSeq.ToString());
        }
        finally
        {
            _sending.Release();
        }
    }

    public void Dispose()
    {
        _player.Changed -= OnPlayerChanged;
        _signals.Changed -= OnSignals;
        if (_bridge is not null)
            _bridge.AppStateChanged -= OnAppState;
    }

    private static ListeningEntry Entry(PlaybackRecord record)
    {
        ListeningEntry entry = record.HeardAt > DateTime.UnixEpoch
            ? ListeningProgress.Heard(record.Id, record.Position, record.Duration, record.HeardAt)
            : new ListeningEntry { SermonId = record.Id, Duration = record.Duration };
        entry.FinishedAt = Latest(entry.FinishedAt, record.Marks.GetValueOrDefault(Finished));
        entry.WatchedAt = record.Marks.TryGetValue(Watched, out DateTime watched) ? watched : null;
        entry.ClearedAt = record.Marks.TryGetValue(Cleared, out DateTime cleared) ? cleared : null;
        return entry;
    }

    private static DateTime? Latest(DateTime? first, DateTime second) =>
        second == default ? first : first is { } at && at > second ? at : second;

    private void OnPlayerChanged()
    {
        bool playing = _player.State.Playing;
        if (_wasPlaying && !playing)
            _ = SendAsync();
        _wasPlaying = playing;
    }

    private void OnSignals(ClientChange change)
    {
        if (!change.Concerns(ChangeKind.Session | ChangeKind.Reconnected, null))
            return;
        bool signedIn = _access.SignedIn;
        bool justSignedIn = signedIn && !_wasSignedIn;
        _wasSignedIn = signedIn;
        _ = SendAsync(everything: justSignedIn);
    }

    private void OnAppState(bool active)
    {
        if (active)
            _ = SendAsync();
    }
}
