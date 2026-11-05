using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.Media;

public sealed class WebMediaFiles(IServiceProvider services) : IMediaFiles
{
    public async ValueTask<string?> SourceOfAsync(MediaRef media) =>
        media.IsPrivate ? await PrivateLinkAsync(services, media) : media.Url;

    public ValueTask ForgetPrivateAsync() => ValueTask.CompletedTask;

    public ValueTask PrefetchAsync(IReadOnlyList<MediaRef> media) => ValueTask.CompletedTask;

    internal static async ValueTask<string?> PrivateLinkAsync(IServiceProvider services, MediaRef media) =>
        services.GetService<IPrivateMediaLinks>() is { } links ? await links.LinkAsync(media.Id, media.Key) : null;
}
