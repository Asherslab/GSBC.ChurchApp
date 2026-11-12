using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Native;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Features.Feedback.Client;

public sealed class ScreenshotPages(NativeBridge bridge, RecentPages recent, IJSRuntime js, TimeProvider time) : IClientStartupTask, IDisposable
{
    private const int MostRemembered = 6;
    private readonly List<ScreenshotTaken> _taken = [];
    private DotNetObjectReference<ScreenshotPages>? _receiver;

    public event Action? Taken;

    public async Task RunAsync()
    {
        if (!bridge.IsNative)
            return;
        _receiver = DotNetObjectReference.Create(this);
        await using IJSObjectReference module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/GSBC.ChurchApp.Features.Feedback.Client/js/feedback-phone.js");
        await module.InvokeVoidAsync("listenForScreenshots", _receiver);
    }

    public IReadOnlyList<ScreenshotTaken> Recent()
    {
        DateTime since = time.GetUtcNow().UtcDateTime - FeedbackPages.ScreenshotsFrom;
        lock (_taken)
            return _taken.Where(taken => taken.At >= since).ToList();
    }

    [JSInvokable]
    public void OnScreenshotTaken()
    {
        lock (_taken)
        {
            _taken.Insert(0, new ScreenshotTaken { Page = recent.Current, At = time.GetUtcNow().UtcDateTime });
            if (_taken.Count > MostRemembered)
                _taken.RemoveAt(MostRemembered);
        }
        Taken?.Invoke();
    }

    public void Dispose() => _receiver?.Dispose();
}
