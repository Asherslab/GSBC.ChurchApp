using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Media;

public sealed class NativeMediaFiles(NativeBridge bridge, IJSRuntime js, IServiceProvider services) : IMediaFiles
{
    public async ValueTask<string?> SourceOfAsync(MediaRef media)
    {
        if (!bridge.IsNative)
            return media.IsPrivate ? await WebMediaFiles.PrivateLinkAsync(services, media) : media.Url;

        if (await js.InvokeAsync<string?>("gsbcNative.cachedMedia", media.Key, media.IsPrivate) is { } cached)
            return cached;

        try
        {
            string? url = media.IsPrivate ? await WebMediaFiles.PrivateLinkAsync(services, media) : media.Url;
            return url is null ? null : await js.InvokeAsync<string?>("gsbcNative.fetchMedia", media.Key, url, media.IsPrivate);
        }
        catch (Exception exception) when (exception is JSException or Grpc.Core.RpcException)
        {
            return null;
        }
    }

    public async ValueTask ForgetPrivateAsync()
    {
        if (bridge.IsNative)
            await js.InvokeVoidAsync("gsbcNative.clearPrivateMedia");
    }
}
