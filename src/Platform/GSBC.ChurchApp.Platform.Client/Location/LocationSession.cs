using System.Text.Json;
using GSBC.ChurchApp.Platform.Client.Native;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Location;

public sealed class LocationSession(IJSRuntime js, IServiceProvider services) : ILocationSession, IAsyncDisposable
{
    private const string Module = "./_content/GSBC.ChurchApp.Platform.Client/location.js";
    private IJSObjectReference? _module;
    private DotNetObjectReference<LocationSession>? _receiver;

    public bool IsAvailable => (services.GetService(typeof(NativeBridge)) as NativeBridge)?.IsNative == true || OperatingSystem.IsBrowser();

    public event Action? FixArrived;

    public async Task<LocationStart> StartAsync(string purpose)
    {
        if (!IsAvailable)
            return LocationStart.Off;
        string state = await (await ModuleAsync()).InvokeAsync<string>("start", purpose);
        return state switch
        {
            "running" => LocationStart.Running,
            "denied" => LocationStart.Denied,
            _ => LocationStart.Off,
        };
    }

    public async Task StopAsync()
    {
        if (IsAvailable)
            await (await ModuleAsync()).InvokeVoidAsync("stop");
    }

    public async Task<LocationStatus> StateAsync()
    {
        if (!IsAvailable)
            return new LocationStatus(false, LocationAuthorization.Off);
        string json = await (await ModuleAsync()).InvokeAsync<string>("state");
        LocationStateJson? state = JsonSerializer.Deserialize(json, LocationJson.Default.LocationStateJson);
        return new LocationStatus(state?.Running == true, state?.Authorization switch
        {
            "notDetermined" => LocationAuthorization.NotDetermined,
            "whenInUse" => LocationAuthorization.WhenInUse,
            "always" => LocationAuthorization.Always,
            "denied" => LocationAuthorization.Denied,
            _ => LocationAuthorization.Off,
        });
    }

    public async Task<LocationFixes> FixesAsync(long since)
    {
        if (!IsAvailable)
            return new LocationFixes([], since, false);
        string json = await (await ModuleAsync()).InvokeAsync<string>("fixes", since);
        LocationFixesJson? found = JsonSerializer.Deserialize(json, LocationJson.Default.LocationFixesJson);
        LocationFix[] fixes = (found?.Fixes ?? [])
            .Select(fix => new LocationFix(fix.Seq, DateTimeOffset.FromUnixTimeMilliseconds(fix.At), fix.Lat, fix.Lon, fix.Accuracy, fix.Heading, fix.Speed))
            .ToArray();
        return new LocationFixes(fixes, found?.Latest ?? since, found?.Running == true);
    }

    public async Task ClearAsync(long upTo)
    {
        if (IsAvailable)
            await (await ModuleAsync()).InvokeVoidAsync("clear", upTo);
    }

    [JSInvokable]
    public void OnFix() => FixArrived?.Invoke();

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
