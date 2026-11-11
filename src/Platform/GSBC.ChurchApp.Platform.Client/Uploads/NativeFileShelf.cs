using System.Text.Json;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Native;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Uploads;

public sealed class NativeFileShelf(NativeBridge bridge, IJSRuntime js) : IFileShelf
{
    public bool IsAvailable => bridge.IsNative;

    public async ValueTask<StagedFile?> KeepAsync(ElementReference input, string contentType)
    {
        if (!IsAvailable)
            return null;
        string? kept = await js.InvokeAsync<string?>("gsbcNative.keepFile", input);
        return kept is null || JsonSerializer.Deserialize(kept, NativeJson.Default.StagedFileJson) is not { } file
            ? null
            : new StagedFile(file.Token, file.Name, contentType, file.Bytes, file.Sha256, file.Preview);
    }

    public async ValueTask ForgetAsync(string token)
    {
        if (IsAvailable)
            await js.InvokeVoidAsync("gsbcNative.forgetFile", token);
    }
}
