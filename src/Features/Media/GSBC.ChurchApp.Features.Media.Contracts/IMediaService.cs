using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Media.Contracts;

[Service("gsbc.media.Media")]
public interface IMediaService
{
    [Requires(MediaPermissions.UploadNode)]
    ValueTask<UploadTicket> BeginUploadAsync(BeginUploadRequest request, CallContext context = default);

    [Requires(MediaPermissions.UploadNode)]
    ValueTask<MediaRef> CompleteUploadAsync(CompleteUploadRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<MediaLink> GetLinkAsync(LinkRequest request, CallContext context = default);

    [Requires(MediaPermissions.ViewNode)]
    ValueTask<MediaList> ListAsync(MediaQuery request, CallContext context = default);
}
