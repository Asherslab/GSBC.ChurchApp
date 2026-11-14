using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Files;

public sealed class FileSaver(IJSRuntime js)
{
    private const string Module = "./_content/GSBC.ChurchApp.Platform.Client/save-file.js";

    public async Task SaveAsync(string name, string contentType, byte[] bytes)
    {
        await using IJSObjectReference module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        await module.InvokeVoidAsync("saveFile", name, contentType, bytes);
    }
}
