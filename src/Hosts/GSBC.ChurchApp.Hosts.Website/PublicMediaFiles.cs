using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Hosts.Website;

public sealed class PublicMediaFiles : IMediaFiles
{
    public ValueTask<string?> SourceOfAsync(MediaRef media) => ValueTask.FromResult(media.IsPrivate ? null : media.Url);

    public ValueTask ForgetPrivateAsync() => ValueTask.CompletedTask;

    public ValueTask PrefetchAsync(IReadOnlyList<MediaRef> media) => ValueTask.CompletedTask;
}
