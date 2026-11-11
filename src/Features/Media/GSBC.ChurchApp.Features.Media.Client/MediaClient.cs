using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Media.Client;

public sealed class MediaClient(ChannelBase channel)
{
    private readonly IMediaService _media = channel.CreateGrpcService<IMediaService>();
    private readonly IMediaFoldersService _folders = channel.CreateGrpcService<IMediaFoldersService>();

    public ValueTask<MediaLink> GetLinkAsync(string id, string key) => _media.GetLinkAsync(new LinkRequest { Id = id, Key = key });

    public ValueTask<MediaList> ListAsync(MediaQuery query) => _media.ListAsync(query);

    public async Task<FolderMap> FoldersAsync() => new(await _folders.ListAsync(new FolderQuery()));
}
