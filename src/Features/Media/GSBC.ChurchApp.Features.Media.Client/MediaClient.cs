using System.Net.Http.Headers;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Media.Client;

public sealed class MediaClient(ChannelBase channel, HttpClient http)
{
    private readonly IMediaService _media = channel.CreateGrpcService<IMediaService>();

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

    public ValueTask<MediaList> ListAsync(int skip, int take) => _media.ListAsync(new MediaQuery { Skip = skip, Take = take });
}
