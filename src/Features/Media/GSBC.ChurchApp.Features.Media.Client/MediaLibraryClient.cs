using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Media.Client;

[AdminOnly]
public sealed class MediaLibraryClient(ChannelBase channel)
{
    private readonly IMediaService _media = channel.CreateGrpcService<IMediaService>();
    private readonly IMediaFoldersService _folders = channel.CreateGrpcService<IMediaFoldersService>();

    public ValueTask<MediaItemInfo> RemoveOriginalAsync(string id) => _media.RemoveOriginalAsync(new RemoveOriginalRequest { Id = id });

    public ValueTask<Done> DeleteAsync(string id) => _media.DeleteAsync(new DeleteMediaRequest { Id = id });

    public ValueTask<FolderInfo> CreateFolderAsync(string? parentId, string name) => _folders.CreateAsync(new CreateFolderRequest { ParentId = parentId, Name = name });

    public ValueTask<Done> RenameFolderAsync(string id, string name) => _folders.RenameAsync(new RenameFolderRequest { Id = id, Name = name });

    public ValueTask<Done> MoveAsync(IEnumerable<string> itemIds, IEnumerable<string> folderIds, string? into) =>
        _folders.MoveAsync(new MoveRequest { ItemIds = [.. itemIds], FolderIds = [.. folderIds], Into = into });

    public ValueTask<Done> DeleteFolderAsync(string id, bool moveContents, string? into) =>
        _folders.DeleteAsync(new DeleteFolderRequest { Id = id, MoveContents = moveContents, MoveContentsInto = into });
}
