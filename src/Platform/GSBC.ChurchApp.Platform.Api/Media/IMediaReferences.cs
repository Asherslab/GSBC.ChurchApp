using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Api.Media;

public interface IMediaReferences
{
    Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken);

    Task UseAsync(string owner, IReadOnlyCollection<MediaRef> verified, CancellationToken cancellationToken);
}
