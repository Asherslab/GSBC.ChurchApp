using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Media.Client;

[AdminOnly]
public sealed class MediaPlacesClient(ChannelBase channel) : IMediaPlaces
{
    private readonly IMediaService _media = channel.CreateGrpcService<IMediaService>();

    public async Task<IReadOnlyDictionary<string, string>> FoldersOfAsync(IReadOnlyCollection<string> mediaIds) =>
        (await _media.PlacesAsync(new MediaPlacesRequest { Ids = [.. mediaIds] })).Items.ToDictionary(place => place.Id, place => place.Folder);
}
