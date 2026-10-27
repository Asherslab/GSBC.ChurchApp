using System.Globalization;
using Gsbc.AppPoc.Data;
using Microsoft.JSInterop;

namespace Gsbc.AppPoc.Native;

public sealed class PlayerService(IJSRuntime js, NativeBridge native) : IAsyncDisposable
{
    private const string PositionKeyPrefix = "position:";
    private const string LengthKeyPrefix = "length:";
    private static readonly TimeSpan SaveEvery = TimeSpan.FromSeconds(5);

    private DotNetObjectReference<PlayerService>? self;
    private DateTime lastSaved;

    public Sermon? Current { get; private set; }
    public double Position { get; private set; }
    public double Duration { get; private set; }
    public bool IsPlaying { get; private set; }

    public event Action? Changed;

    public async Task StartAsync()
    {
        self ??= DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("gsbc.player.listen", self);
    }

    public bool IsCurrent(Sermon sermon) => Current?.Id == sermon.Id;

    public async Task<double> SavedPositionAsync(Sermon sermon) =>
        double.TryParse(await native.GetAsync(PositionKeyPrefix + sermon.Id), CultureInfo.InvariantCulture, out var saved) ? saved : 0;

    public async Task PlayAsync(Sermon sermon)
    {
        if (sermon.AudioUrl is null)
        {
            return;
        }
        if (IsCurrent(sermon))
        {
            await ToggleAsync();
            return;
        }
        Current = sermon;
        Position = await SavedPositionAsync(sermon);
        Duration = 0;
        Changed?.Invoke();
        IsPlaying = await js.InvokeAsync<bool>("gsbc.player.load", sermon.Id, sermon.AudioUrl, sermon.Title, sermon.Speaker, sermon.ImageUrl ?? "", Position);
        Changed?.Invoke();
    }

    public async Task ToggleAsync() => IsPlaying = await js.InvokeAsync<bool>("gsbc.player.toggle");

    public ValueTask SkipAsync(int seconds) => js.InvokeVoidAsync("gsbc.player.skip", seconds);

    public ValueTask SeekAsync(double seconds) => js.InvokeVoidAsync("gsbc.player.seek", seconds);

    [JSInvokable]
    public async Task OnTick(string id, double position, double duration, bool playing)
    {
        if (Current?.Id != id)
        {
            return;
        }
        Position = position;
        Duration = duration;
        IsPlaying = playing;
        if (DateTime.UtcNow - lastSaved > SaveEvery && duration > 0)
        {
            lastSaved = DateTime.UtcNow;
            await native.SetAsync(PositionKeyPrefix + id, position.ToString(CultureInfo.InvariantCulture));
            await native.SetAsync(LengthKeyPrefix + id, duration.ToString(CultureInfo.InvariantCulture));
        }
        Changed?.Invoke();
    }

    public static string Clock(double seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
    }

    public ValueTask DisposeAsync()
    {
        self?.Dispose();
        return ValueTask.CompletedTask;
    }
}
