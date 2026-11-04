using System.Net.Http.Headers;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Media.Client;

public sealed class MediaClient(ChannelBase channel, HttpClient http)
{
    private readonly IMediaService _media = channel.CreateGrpcService<IMediaService>();
    private readonly IMediaFoldersService _folders = channel.CreateGrpcService<IMediaFoldersService>();

    public async Task<MediaRef> UploadAsync(BeginUploadRequest request, Stream content, CancellationToken cancellationToken = default)
    {
        UploadTicket ticket = await _media.BeginUploadAsync(request);

        using StreamContent body = new(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(ticket.ContentType);
        using HttpResponseMessage response = await http.PutAsync(ticket.Url, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new RpcException(new Status(StatusCode.Unavailable, $"The file couldn't be sent ({(int)response.StatusCode})."));

        return await _media.CompleteUploadAsync(new CompleteUploadRequest { UploadId = ticket.UploadId });
    }

    public ValueTask<MediaLink> GetLinkAsync(string id) => _media.GetLinkAsync(new LinkRequest { Id = id });

    public ValueTask<MediaList> ListAsync(MediaQuery query) => _media.ListAsync(query);

    public ValueTask<Done> DeleteAsync(string id) => _media.DeleteAsync(new DeleteMediaRequest { Id = id });

    public async Task<FolderMap> FoldersAsync() => new(await _folders.ListAsync(new FolderQuery()));

    public ValueTask<FolderInfo> CreateFolderAsync(string? parentId, string name) => _folders.CreateAsync(new CreateFolderRequest { ParentId = parentId, Name = name });

    public ValueTask<Done> RenameFolderAsync(string id, string name) => _folders.RenameAsync(new RenameFolderRequest { Id = id, Name = name });

    public ValueTask<Done> MoveAsync(IEnumerable<string> itemIds, IEnumerable<string> folderIds, string? into) =>
        _folders.MoveAsync(new MoveRequest { ItemIds = [.. itemIds], FolderIds = [.. folderIds], Into = into });

    public ValueTask<Done> DeleteFolderAsync(string id, bool moveContents, string? into) =>
        _folders.DeleteAsync(new DeleteFolderRequest { Id = id, MoveContents = moveContents, MoveContentsInto = into });
}
