using GSBC.ChurchApp.Features.Media.Contracts;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class MediaFoldersService(MediaFolders folders) : IMediaFoldersService
{
    public async ValueTask<FolderTree> ListAsync(FolderQuery request, CallContext context = default) =>
        await folders.ListAsync(MediaService.UserOf(context), context.CancellationToken);

    public async ValueTask<FolderInfo> CreateAsync(CreateFolderRequest request, CallContext context = default) =>
        await folders.CreateAsync(MediaService.UserOf(context), request, context.CancellationToken);

    public async ValueTask<Done> RenameAsync(RenameFolderRequest request, CallContext context = default)
    {
        await folders.RenameAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> MoveAsync(MoveRequest request, CallContext context = default)
    {
        await folders.MoveAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteAsync(DeleteFolderRequest request, CallContext context = default)
    {
        await folders.DeleteAsync(request, context.CancellationToken);
        return new Done();
    }
}
