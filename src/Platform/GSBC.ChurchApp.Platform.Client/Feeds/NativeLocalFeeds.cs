using System.Text.Json;
using GSBC.ChurchApp.Platform.Client.Native;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Feeds;

public sealed class NativeLocalFeeds(NativeBridge bridge, IJSRuntime js) : ILocalFeeds, IDisposable
{
    private DotNetObjectReference<NativeLocalFeeds>? _receiver;

    public bool IsAvailable => bridge.IsNative;

    public SyncState State { get; private set; } = SyncState.Stopped;

    public event Action<IReadOnlySet<string>>? FeedsChanged;

    public event Action? StateChanged;

    public async Task StartAsync(IReadOnlyCollection<string> feeds, IReadOnlyCollection<string> privateFeeds)
    {
        if (!IsAvailable)
            return;

        _receiver = DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("gsbcNative.listen", _receiver);
        State = Parse(await js.InvokeAsync<string>("gsbcNative.status"));
        await js.InvokeVoidAsync("gsbcNative.startSync", feeds, privateFeeds);
    }

    public async ValueTask<IReadOnlyList<StoredRecord>> ReadPageAsync(FeedPageQuery query)
    {
        if (!IsAvailable)
            return [];

        string json = await js.InvokeAsync<string>("gsbcNative.readPage", query.Feed,
            query.Order == FeedOrder.StartsAt ? "startsAt" : "sortKey", query.Descending, query.Limit, query.Offset, query.GroupKey);

        return (JsonSerializer.Deserialize(json, NativeJson.Default.StoredRecordJsonArray) ?? [])
            .Select(record => new StoredRecord(record.Id, record.Version, Convert.FromBase64String(record.Payload),
                record.SortKey, FromMilliseconds(record.StartsAt), FromMilliseconds(record.EndsAt), record.GroupKey))
            .ToList();
    }

    public async ValueTask<StoreInfo> InfoAsync()
    {
        if (!IsAvailable)
            return new StoreInfo(0, []);

        StoreInfoJson? info = JsonSerializer.Deserialize(await js.InvokeAsync<string>("gsbcNative.storeInfo"), NativeJson.Default.StoreInfoJson);
        return new StoreInfo(info?.FileBytes ?? 0,
            info?.Feeds.Select(feed => new StoredFeed(feed.Feed, feed.Rows, feed.Bytes, feed.Cursor)).ToList() ?? []);
    }

    public void Follow(IEnumerable<string> feeds)
    {
    }

    public async ValueTask RefreshAsync()
    {
        if (IsAvailable)
            await js.InvokeVoidAsync("gsbcNative.pull");
    }

    [JSInvokable]
    public void OnFeedsChanged(string[] feeds) => FeedsChanged?.Invoke(feeds.ToHashSet());

    [JSInvokable]
    public void OnSyncState(string state)
    {
        State = Parse(state);
        StateChanged?.Invoke();
    }

    public void Dispose() => _receiver?.Dispose();

    private static SyncState Parse(string state) =>
        Enum.TryParse(state, ignoreCase: true, out SyncState parsed) ? parsed : SyncState.Stopped;

    private static DateTime? FromMilliseconds(long? milliseconds) =>
        milliseconds is { } value ? DateTime.UnixEpoch.AddMilliseconds(value) : null;
}
