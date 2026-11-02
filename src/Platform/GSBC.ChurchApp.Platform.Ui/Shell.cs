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

public sealed class Shell(IJSRuntime js)
{
    public ValueTask HapticAsync(Haptic kind = Haptic.Light) => js.InvokeVoidAsync("gsbcShell.haptic", kind.ToString().ToLowerInvariant());

    public ValueTask StartedAsync() => js.InvokeVoidAsync("gsbcShell.started");

    public ValueTask BackAsync(string fallback) => js.InvokeVoidAsync("gsbcShell.back", fallback);

    public ValueTask ScrollToTopAsync() => js.InvokeVoidAsync("gsbcShell.scrollToTop");

    public ValueTask OpenExternalAsync(string url) => js.InvokeVoidAsync("gsbcShell.openExternal", url);

    internal ValueTask<IJSObjectReference> PullToRefreshAsync(ElementReference host, DotNetObjectReference<PullToRefresh> receiver) =>
        js.InvokeAsync<IJSObjectReference>("gsbcShell.pullToRefresh", host, receiver);
}
