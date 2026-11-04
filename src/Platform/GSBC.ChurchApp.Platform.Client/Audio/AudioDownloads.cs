using System.Text.Json;
using GSBC.ChurchApp.Platform.Client.Native;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Audio;

public sealed class AudioDownloads(IJSRuntime js, IServiceProvider services) : IAudioDownloads, IAsyncDisposable
{
    private const string Module = "./_content/GSBC.ChurchApp.Platform.Client/audio.js";
    private IJSObjectReference? _module;
    private DotNetObjectReference<AudioDownloads>? _receiver;

    public bool IsAvailable => (services.GetService(typeof(NativeBridge)) as NativeBridge)?.IsNative == true;

    public event Action? Changed;

    public async Task<IReadOnlyDictionary<string, AudioDownload>> ListAsync()
    {
        if (!IsAvailable)
            return new Dictionary<string, AudioDownload>();
        string json = await (await ModuleAsync()).InvokeAsync<string>("downloads");
        return (JsonSerializer.Deserialize(json, AudioJson.Default.AudioDownloadJsonArray) ?? [])
            .Select(entry => new AudioDownload(entry.Id, entry.Title, entry.Bytes, entry.Expected, entry.State switch
            {
                "done" => DownloadState.Done,
                "failed" => DownloadState.Failed,
                _ => DownloadState.Downloading,
            }))
            .ToDictionary(download => download.Id);
    }

    public async Task DownloadAsync(AudioItem item)
    {
        if (IsAvailable)
            await (await ModuleAsync()).InvokeVoidAsync("download", item.Id, item.Url, item.Title);
    }

    public async Task RemoveAsync(string id)
    {
        if (IsAvailable)
            await (await ModuleAsync()).InvokeVoidAsync("removeDownload", id);
    }

    [JSInvokable]
    public void OnDownloadsChanged() => Changed?.Invoke();

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
        await module.InvokeVoidAsync("listenDownloads", _receiver);
        return _module = module;
    }
}
