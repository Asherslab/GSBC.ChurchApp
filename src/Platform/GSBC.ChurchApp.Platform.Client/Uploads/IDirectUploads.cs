using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Client.Uploads;

public interface IDirectUploads
{
    Task<MediaRef> UploadAsync(Stream content, string fileName, string contentType, long bytes, UploadTarget target, CancellationToken cancellationToken = default);
}
