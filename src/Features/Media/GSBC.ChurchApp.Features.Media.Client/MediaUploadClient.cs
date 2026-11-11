using System.Net.Http.Headers;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Media.Client;

[AdminOnly]
public sealed class MediaUploadClient(ChannelBase channel, HttpClient http)
{
    private readonly IMediaService _media = channel.CreateGrpcService<IMediaService>();

    public async Task<UploadedMedia> UploadAsync(BeginUploadRequest request, Stream content, bool addAgain = false, string? putBackInto = null, CancellationToken cancellationToken = default)
    {
        UploadTicket ticket = await _media.BeginUploadAsync(request);

        using StreamContent body = new(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(ticket.ContentType);
        using HttpResponseMessage response = await http.PutAsync(ticket.Url, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new RpcException(new Status(StatusCode.Unavailable, $"The file couldn't be sent ({(int)response.StatusCode})."));

        return await _media.CompleteUploadAsync(new CompleteUploadRequest { UploadId = ticket.UploadId, AddAgain = addAgain, PutBackInto = putBackInto });
    }

    public async Task<MediaMatches> FindAsync(string sha256, string contentType, MediaAudience? privateTo) =>
        await _media.FindByHashAsync(new FindByHashRequest { Sha256 = sha256, ContentType = contentType, PrivateTo = privateTo });
}
