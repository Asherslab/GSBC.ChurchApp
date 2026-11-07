using Grpc.Core;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

internal sealed class Library : IMediaReferences
{
    private readonly Dictionary<string, MediaRef> _items = [];
    private readonly HashSet<string> _formerKeys = [];

    public MediaRef Add(MediaRef media) => _items[media.Id] = media;

    public MediaRef Replace(MediaRef media)
    {
        _formerKeys.Add(_items[media.Id].Key);
        return Add(media);
    }

    public Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaRef>>(claimed
            .Select(media => _items.TryGetValue(media.Id, out MediaRef? known) && (media.Key.Length == 0 || media.Key == known.Key || _formerKeys.Contains(media.Key))
                ? known.IsPrivate ? throw new RpcException(new Status(StatusCode.InvalidArgument, "Settings need a public image.")) : known
                : throw new RpcException(new Status(StatusCode.InvalidArgument, "Not in the library.")))
            .ToList());

    public Task UseAsync(string owner, IReadOnlyCollection<MediaRef> verified, CancellationToken cancellationToken) => Task.CompletedTask;
}
