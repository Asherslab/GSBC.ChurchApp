using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Preferences;

internal sealed class NativeLocalPreferences(IJSRuntime js) : ILocalPreferences
{
    public ValueTask<string?> GetAsync(string key) => js.InvokeAsync<string?>("gsbcNative.preference", key);

    public ValueTask SetAsync(string key, string? value) => js.InvokeVoidAsync("gsbcNative.setPreference", key, value);
}
