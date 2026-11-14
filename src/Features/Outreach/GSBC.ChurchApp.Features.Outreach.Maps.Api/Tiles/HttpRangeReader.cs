using System.Net;
using System.Net.Http.Headers;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;

internal sealed class HttpRangeReader(HttpClient http, Uri archive) : IRangeReader
{
    private const int Attempts = 4;

    public async Task<byte[]> ReadAsync(long offset, long length, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, archive);
                request.Headers.Range = new RangeHeaderValue(offset, offset + length - 1);
                using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
                if (response.StatusCode != HttpStatusCode.PartialContent)
                    throw new HttpRequestException($"{archive} answered {(int)response.StatusCode} to a range request");
                byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                if (bytes.Length != length)
                    throw new HttpRequestException($"{archive} sent {bytes.Length} bytes of {length}");
                return bytes;
            }
            catch (HttpRequestException) when (attempt < Attempts && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5 * attempt), cancellationToken);
            }
        }
    }
}
