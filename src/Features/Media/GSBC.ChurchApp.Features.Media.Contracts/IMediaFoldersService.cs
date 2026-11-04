using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Media.Contracts;

[Service("gsbc.media.Folders")]
public interface IMediaFoldersService
{
    [Requires(MediaPermissions.ViewNode)]
    ValueTask<FolderTree> ListAsync(FolderQuery request, CallContext context = default);

    [Requires(MediaPermissions.OrganiseNode)]
    ValueTask<FolderInfo> CreateAsync(CreateFolderRequest request, CallContext context = default);

    [Requires(MediaPermissions.OrganiseNode)]
    ValueTask<Done> RenameAsync(RenameFolderRequest request, CallContext context = default);

    [Requires(MediaPermissions.OrganiseNode)]
    ValueTask<Done> MoveAsync(MoveRequest request, CallContext context = default);

    [Requires(MediaPermissions.OrganiseNode)]
    ValueTask<Done> DeleteAsync(DeleteFolderRequest request, CallContext context = default);
}
