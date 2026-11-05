using System.Collections.Concurrent;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Client.Media;

namespace GSBC.ChurchApp.Features.Media.Client;

internal sealed class PrivateMediaLinks(MediaClient media, TimeProvider time) : IPrivateMediaLinks
{
    private static readonly TimeSpan Margin = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<string, MediaLink> _links = new();

    public async ValueTask<string?> LinkAsync(string mediaId, string key)
    {
        if (_links.TryGetValue(key, out MediaLink? known) && known.ExpiresAt - Margin > time.GetUtcNow().UtcDateTime)
            return known.Url;

        try
        {
            MediaLink link = await media.GetLinkAsync(mediaId, key);
            _links[key] = link;
            return link.Url;
        }
        catch (RpcException exception) when (exception.StatusCode is StatusCode.PermissionDenied or StatusCode.Unauthenticated or StatusCode.NotFound)
        {
            _links.TryRemove(key, out _);
            return null;
        }
    }
}
