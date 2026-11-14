using System.Text.Json;
using GSBC.ChurchApp.Platform.Client.Native;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Files;

public sealed class KeptFiles(IJSRuntime js, IServiceProvider services) : IKeptFiles, IAsyncDisposable
{
    private const string Module = "./_content/GSBC.ChurchApp.Platform.Client/kept-files.js";
    private IJSObjectReference? _module;
    private DotNetObjectReference<KeptFiles>? _receiver;

    public bool IsAvailable => (services.GetService(typeof(NativeBridge)) as NativeBridge)?.IsNative == true;

    public event Action? Changed;

    public async Task KeepAsync(string id, string url, string title)
    {
        if (IsAvailable)
            await (await ModuleAsync()).InvokeVoidAsync("keep", id, url, title);
    }

    public async Task<IReadOnlyList<KeptFile>> ListAsync()
    {
        if (!IsAvailable)
            return [];
        string json = await (await ModuleAsync()).InvokeAsync<string>("list");
        return (JsonSerializer.Deserialize(json, KeptFilesJson.Default.KeptFileJsonArray) ?? [])
            .Select(entry => new KeptFile(entry.Id, entry.Url ?? "", entry.Title ?? "", entry.Bytes, entry.Expected, entry.State switch
            {
                "done" => KeptFileState.Done,
                "failed" => KeptFileState.Failed,
                _ => KeptFileState.Downloading,
            }))
            .ToArray();
    }

    public async Task RemoveAsync(string id)
    {
        if (IsAvailable)
            await (await ModuleAsync()).InvokeVoidAsync("remove", id);
    }

    public async Task<byte[]> ReadAsync(string id, long offset = 0, long length = -1)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Kept files are only on the phone.");
        return await (await ModuleAsync()).InvokeAsync<byte[]>("readKept", id, offset, length);
    }

    [JSInvokable]
    public void OnKeptFilesChanged() => Changed?.Invoke();

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
