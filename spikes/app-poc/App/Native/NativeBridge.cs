using Gsbc.AppPoc.Components;
using Microsoft.JSInterop;

namespace Gsbc.AppPoc.Native;

public enum Haptic { Selection, Light, Medium, Success, Warning }

public enum DisplayMode { Native, Standalone, BrowserIos, BrowserInstallable, Browser }

public sealed record CalendarEntry(string Title, DateTime Start, DateTime End, string Location, string Notes);

public sealed class NativeBridge(IJSRuntime js)
{
    public ValueTask<bool> IsNativeAsync() => js.InvokeAsync<bool>("gsbc.isNative");

    public ValueTask<string> PlatformAsync() => js.InvokeAsync<string>("gsbc.platform");

    public async ValueTask HapticAsync(Haptic kind) => await TryHapticAsync(kind);

    public ValueTask<string> TryHapticAsync(Haptic kind) => js.InvokeAsync<string>("gsbc.haptic", kind.ToString().ToLowerInvariant());

    public async ValueTask<DisplayMode> DisplayModeAsync() =>
        await js.InvokeAsync<string>("gsbc.displayMode") switch
        {
            "native" => DisplayMode.Native,
            "standalone" => DisplayMode.Standalone,
            "browser-ios" => DisplayMode.BrowserIos,
            "browser-installable" => DisplayMode.BrowserInstallable,
            _ => DisplayMode.Browser,
        };

    public async ValueTask<bool> ShouldOfferInstallAsync() => await DisplayModeAsync() is DisplayMode.BrowserIos or DisplayMode.BrowserInstallable;

    public ValueTask<bool> InstallAsync() => js.InvokeAsync<bool>("gsbc.install");

    public ValueTask OnUpdateReadyAsync(DotNetObjectReference<UpdateBanner> listener) =>
        js.InvokeVoidAsync("gsbc.updates.onReady", listener);

    public ValueTask ApplyUpdateAsync() => js.InvokeVoidAsync("gsbc.updates.apply");

    public async ValueTask<bool> IsIosAppAsync() => await IsNativeAsync() && await PlatformAsync() == "ios";

    public ValueTask ShareAsync(string title, string text, string url) => js.InvokeVoidAsync("gsbc.share", title, text, url);

    public ValueTask OpenInAppAsync(string url) => js.InvokeVoidAsync("gsbc.openInApp", url);

    public ValueTask OpenExternalAsync(string url) => js.InvokeVoidAsync("gsbc.openExternal", url);

    public ValueTask<string> AddToCalendarAsync(CalendarEntry entry) =>
        js.InvokeAsync<string>("gsbc.addToCalendar", entry.Title, ToEpochMs(entry.Start), ToEpochMs(entry.End), entry.Location, entry.Notes);

    public ValueTask<string> ScheduleNotificationAsync(string title, string body, int secondsFromNow) =>
        js.InvokeAsync<string>("gsbc.scheduleNotification", title, body, secondsFromNow);

    public ValueTask<string?> GetAsync(string key) => js.InvokeAsync<string?>("gsbc.get", key);

    public ValueTask SetAsync(string key, string value) => js.InvokeVoidAsync("gsbc.set", key, value);

    public ValueTask ScrollToTopAsync() => js.InvokeVoidAsync("gsbc.scrollToTop");

    public ValueTask ApplyThemeAsync(Theme theme, Appearance appearance, string layout) =>
        js.InvokeVoidAsync("gsbc.applyTheme", theme.ToString(), appearance.ToString(), layout);

    public ValueTask MarkFirstRenderAsync() => js.InvokeVoidAsync("gsbc.markFirstRender");

    public ValueTask BackAsync(string fallback) => js.InvokeVoidAsync("gsbc.back", fallback);

    public ValueTask<string> PositionsJsonAsync() => js.InvokeAsync<string>("gsbc.positions");

    public ValueTask<string> ProbeAsync(string name) => js.InvokeAsync<string>("gsbc.probe", name);

    private static double ToEpochMs(DateTime local) => new DateTimeOffset(local).ToUnixTimeMilliseconds();
}
