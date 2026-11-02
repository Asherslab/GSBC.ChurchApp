using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Api.Media;

public interface IMediaReferences
{
    Task<IReadOnlyList<MediaRef>> AttachAsync(string owner, IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken);

    Task DetachAsync(string owner, CancellationToken cancellationToken);
}
