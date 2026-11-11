using System.Text.Json;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Native;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Uploads;

public sealed class NativeFileShelf(NativeBridge bridge, IJSRuntime js) : IFileShelf
{
    public bool IsAvailable => bridge.IsNative;

    public async ValueTask<StagedFile?> KeepAsync(ElementReference input, string contentType, int index = 0) =>
        IsAvailable ? Staged(await js.InvokeAsync<string?>("gsbcNative.keepFile", input, index), contentType) : null;

    public async ValueTask<StagedFile?> CopyStoreAsync(string contentType) =>
        IsAvailable ? Staged(await js.InvokeAsync<string?>("gsbcNative.copyStore"), contentType) : null;

    private static StagedFile? Staged(string? kept, string contentType) =>
        kept is null || JsonSerializer.Deserialize(kept, NativeJson.Default.StagedFileJson) is not { } file
            ? null
            : new StagedFile(file.Token, file.Name, contentType, file.Bytes, file.Sha256, file.Preview);

    public async ValueTask ForgetAsync(string token)
    {
        if (IsAvailable)
            await js.InvokeVoidAsync("gsbcNative.forgetFile", token);
    }
}
