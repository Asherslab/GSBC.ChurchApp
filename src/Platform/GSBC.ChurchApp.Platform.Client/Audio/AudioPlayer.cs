using System.Text.Json;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Audio;

public sealed class AudioPlayer(IJSRuntime js) : IAudioPlayer, IAsyncDisposable
{
    private const string Module = "./_content/GSBC.ChurchApp.Platform.Client/audio.js";

    private readonly SemaphoreSlim _starting = new(1, 1);
    private IJSObjectReference? _module;
    private DotNetObjectReference<AudioPlayer>? _receiver;

    private IReadOnlyList<AudioItem> _upNext = [];

    public AudioState State { get; private set; } = AudioState.Idle;

    public IReadOnlyList<AudioItem> UpNext => _upNext;

    public event Action? Changed;

    public event Action<AudioItem>? Advanced;

    public async Task StartAsync() => await ModuleAsync();

    public async Task LoadAsync(AudioItem item, double startAt, bool play) =>
        await (await ModuleAsync()).InvokeVoidAsync("load", JsonSerializer.Serialize(Json(item), AudioJson.Default.AudioItemJson), startAt, play);

    public Task PlayAsync() => CallAsync("play");

    public Task PauseAsync() => CallAsync("pause");

    public async Task SeekAsync(double position) => await (await ModuleAsync()).InvokeVoidAsync("seek", position);

    public async Task SkipAsync(double seconds) => await (await ModuleAsync()).InvokeVoidAsync("skip", seconds);

    public async Task SetRateAsync(double rate) => await (await ModuleAsync()).InvokeVoidAsync("rate", rate);

    public async Task SetUpNextAsync(IReadOnlyList<AudioItem> items)
    {
        _upNext = items;
        await (await ModuleAsync()).InvokeVoidAsync("upNext", JsonSerializer.Serialize(items.Select(Json).ToArray(), AudioJson.Default.AudioItemJsonArray));
    }

    public Task StopAsync() => CallAsync("stop");

    public async Task MarkAsync(string id, string mark, DateTime at) =>
        await (await ModuleAsync()).InvokeVoidAsync("mark", id, mark, new DateTimeOffset(at.ToUniversalTime()).ToUnixTimeMilliseconds());

    public async Task<PlaybackJournal> JournalAsync(long since)
    {
        string json = await (await ModuleAsync()).InvokeAsync<string>("journal", since);
        PlaybackJournalJson? journal = JsonSerializer.Deserialize(json, AudioJson.Default.PlaybackJournalJson);
        return journal is null
            ? new PlaybackJournal([], since)
            : new PlaybackJournal(journal.Records.Select(record => new PlaybackRecord(
                record.Id, record.Position, record.Duration, Time(record.HeardAt),
                (record.Marks ?? []).ToDictionary(mark => mark.Key, mark => Time(mark.Value)), record.Seq)).ToList(), journal.LatestSeq);
    }

    [JSInvokable]
    public void OnAudioState(string json)
    {
        if (JsonSerializer.Deserialize(json, AudioJson.Default.AudioStateJson) is not { } state)
            return;
        AudioItem? item = state.Item is { } current ? Item(current) : null;
        bool advanced = item is not null && State.Item is { } before && before.Id != item.Id && !State.Ended && State.Playing;
        State = new AudioState(item, state.Playing, state.Buffering, state.Position, state.Duration > 0 ? state.Duration : item?.Duration ?? 0, state.Rate,
            state.Ended, state.Problem);
        if (advanced)
        {
            _upNext = _upNext.SkipWhile(next => next.Id != item!.Id).Skip(1).ToList();
            Advanced?.Invoke(item!);
        }
        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
            await _module.DisposeAsync();
        _receiver?.Dispose();
    }

    private async Task CallAsync(string method) => await (await ModuleAsync()).InvokeVoidAsync(method);

    private async Task<IJSObjectReference> ModuleAsync()
    {
        if (_module is not null)
            return _module;
        await _starting.WaitAsync();
        try
        {
            if (_module is not null)
                return _module;
            IJSObjectReference module = await js.InvokeAsync<IJSObjectReference>("import", Module);
            _receiver = DotNetObjectReference.Create(this);
            OnAudioState(await module.InvokeAsync<string>("start", _receiver));
            _module = module;
            return module;
        }
        finally
        {
            _starting.Release();
        }
    }

    private static AudioItemJson Json(AudioItem item) => new(item.Id, item.Url, item.Title, item.Artist, item.Album, item.ArtworkUrl, item.Duration, item.Link);

    private static AudioItem Item(AudioItemJson item) => new(item.Id, item.Url, item.Title, item.Artist, item.Album, item.ArtworkUrl, item.Duration, item.Link);

    private static DateTime Time(long milliseconds) => DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
}
