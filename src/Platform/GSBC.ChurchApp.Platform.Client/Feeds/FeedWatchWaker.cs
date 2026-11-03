using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Feeds;

internal sealed class FeedWatchWaker(FeedWatch watch, IJSRuntime js) : IClientStartupTask, IDisposable
{
    private DotNetObjectReference<FeedWatchWaker>? _receiver;

    public async Task RunAsync()
    {
        _receiver = DotNetObjectReference.Create(this);
        await using IJSObjectReference module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/GSBC.ChurchApp.Platform.Client/feed-watch.js");
        await module.InvokeVoidAsync("wakeOnReturn", _receiver);
    }

    [JSInvokable]
    public void Wake() => watch.Wake();

    public void Dispose() => _receiver?.Dispose();
}
