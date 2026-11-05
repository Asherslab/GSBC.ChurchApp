using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Api.Media;

public interface IMediaSystemFolders
{
    Task FileAsync(MediaSystemArea area, string? scopeId, string scopeName, IReadOnlyCollection<MediaRef> media, CancellationToken cancellationToken);

    Task<MediaRef> KeepAsync(MediaSystemArea area, KeptFile file, CancellationToken cancellationToken);

    Task<MediaRef> KeepStoredAsync(MediaSystemArea area, StoredFile file, CancellationToken cancellationToken);
}
