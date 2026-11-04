using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Features.Media.Client;

public sealed class MediaFolderMemory(IJSRuntime js)
{
    private const string Key = "gsbc:media-folder";

    private string? _folderId;
    private bool _read;

    public async Task<string?> LastFolderAsync()
    {
        if (_read)
            return _folderId;
        _read = true;
        try
        {
            _folderId = await js.InvokeAsync<string?>("localStorage.getItem", Key);
        }
        catch (JSException)
        {
            _folderId = null;
        }
        return _folderId;
    }

    public Task<bool> RememberAsync(string? folderId)
    {
        _folderId = folderId;
        _read = true;
        return folderId is null ? StoreAsync("localStorage.removeItem", Key) : StoreAsync("localStorage.setItem", Key, folderId);
    }

    private async Task<bool> StoreAsync(string identifier, params object?[] arguments)
    {
        try
        {
            await js.InvokeVoidAsync(identifier, arguments);
            return true;
        }
        catch (JSException)
        {
            return false;
        }
    }
}
