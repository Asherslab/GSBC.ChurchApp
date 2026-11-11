using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using GSBC.ChurchApp.Platform.Client.Uploads;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Media.Client;

[AdminOnly]
public sealed class MediaDirectUploads(MediaUploadClient uploads) : IDirectUploads
{
    public async Task<MediaRef> UploadAsync(Stream content, string fileName, string contentType, long bytes, UploadTarget target, CancellationToken cancellationToken = default)
    {
        UploadedMedia uploaded = await uploads.UploadAsync(new BeginUploadRequest
        {
            FileName = fileName, ContentType = contentType, Bytes = bytes, AltText = target.AltText,
            PrivateTo = target.PrivateTo is { } permission ? new MediaAudience { Permission = permission, ScopeId = target.PrivateToScope } : null,
            Area = target.Area, AreaScopeId = target.AreaScopeId,
        }, content, cancellationToken: cancellationToken);
        return uploaded.Item?.Media ?? throw new RpcException(new Status(StatusCode.Internal, $"{fileName} was sent but the library didn't say where it went."));
    }
}
