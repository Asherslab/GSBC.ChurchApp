using System.Text.Json;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Location;

public sealed class WalkActivity(IJSRuntime js) : IWalkActivity, IAsyncDisposable
{
    private const string Module = "./_content/GSBC.ChurchApp.Platform.Client/walk-activity.js";
    private IJSObjectReference? _module;
    private DotNetObjectReference<WalkActivity>? _receiver;

    public event Action? TapArrived;

    public async Task ShowAsync(string name, WalkActivityState state, string? alert) =>
        await (await ModuleAsync()).InvokeVoidAsync("show", name, JsonSerializer.Serialize(state, LocationJson.Default.WalkActivityState), alert);

    public async Task EndAsync() => await (await ModuleAsync()).InvokeVoidAsync("end");

    public async Task<IReadOnlyList<WalkTap>> TapsAsync(long since)
    {
        string json = await (await ModuleAsync()).InvokeAsync<string>("taps", since);
        WalkTapJson[] taps = JsonSerializer.Deserialize(json, LocationJson.Default.WalkTapJsonArray) ?? [];
        return [.. taps.Select(tap => new WalkTap(tap.Seq, tap.Kind, DateTimeOffset.FromUnixTimeMilliseconds(tap.At)))];
    }

    public async Task ClearTapsAsync(long upTo) => await (await ModuleAsync()).InvokeVoidAsync("clear", upTo);

    [JSInvokable]
    public void OnTap() => TapArrived?.Invoke();

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
            await _module.DisposeAsync();
        _receiver?.Dispose();
    }

    private async Task<IJSObjectReference> ModuleAsync()
    {
        if (_module is not null)
            return _module;
        IJSObjectReference module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        _receiver = DotNetObjectReference.Create(this);
        await module.InvokeVoidAsync("listen", _receiver);
        return _module = module;
    }
}
