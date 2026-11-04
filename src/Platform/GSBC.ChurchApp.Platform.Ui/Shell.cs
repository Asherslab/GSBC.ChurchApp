using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Ui;

public enum Haptic
{
    Light,
    Medium,
    Success,
    Warning,
}

public sealed class Shell(IJSRuntime js) : IDisposable
{
    private DotNetObjectReference<Shell>? _receiver;

    public event Action? Returned;

    public async ValueTask WatchReturnsAsync()
    {
        if (_receiver is not null)
            return;
        _receiver = DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("gsbcShell.onReturn", _receiver);
    }

    [JSInvokable]
    public void OnReturned() => Returned?.Invoke();

    public void Dispose() => _receiver?.Dispose();

    public ValueTask HapticAsync(Haptic kind = Haptic.Light) => js.InvokeVoidAsync("gsbcShell.haptic", kind.ToString().ToLowerInvariant());

    public ValueTask FitToSpaceAsync(ElementReference frame, int width, int height) =>
        js.InvokeVoidAsync("gsbcShell.fitToSpace", frame, width, height);

    public ValueTask ShowModalAsync(ElementReference dialog) => js.InvokeVoidAsync("gsbcShell.showModal", dialog);

    public ValueTask OpenWindowAsync(string url, string name, int width, int height) =>
        js.InvokeVoidAsync("gsbcShell.openWindow", url, name, width, height);

    public ValueTask TellOtherWindowsAsync(string channel, string message) => js.InvokeVoidAsync("gsbcShell.tell", channel, message);

    public async ValueTask<WindowChannel> HearAsync(string channel, Func<string, Task> heard)
    {
        WindowChannel listener = new(heard);
        listener.Subscription = await js.InvokeAsync<IJSObjectReference>("gsbcShell.hear", channel, listener.Receiver);
        return listener;
    }

    public async ValueTask<WindowChannel> HearUndoKeysAsync(Func<string, Task> heard)
    {
        WindowChannel listener = new(heard);
        listener.Subscription = await js.InvokeAsync<IJSObjectReference>("gsbcShell.undoKeys", listener.Receiver);
        return listener;
    }

    public ValueTask StartedAsync() => js.InvokeVoidAsync("gsbcShell.started");

    public ValueTask BackAsync(string fallback) => js.InvokeVoidAsync("gsbcShell.back", fallback);

    public ValueTask ScrollToTopAsync() => js.InvokeVoidAsync("gsbcShell.scrollToTop");

    public ValueTask OpenExternalAsync(string url) => js.InvokeVoidAsync("gsbcShell.openExternal", url);

    public ValueTask<bool> CopyAsync(string text) => js.InvokeAsync<bool>("gsbcShell.copy", text);

    internal ValueTask<IJSObjectReference> PullToRefreshAsync(ElementReference host, DotNetObjectReference<PullToRefresh> receiver) =>
        js.InvokeAsync<IJSObjectReference>("gsbcShell.pullToRefresh", host, receiver);
}
