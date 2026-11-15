using System.Net;
using System.Net.Http.Headers;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;

internal sealed class HttpRangeReader(HttpClient http, Uri archive) : IRangeReader
{
    private const int Attempts = 5;
    private const long SlowestBytesPerSecond = 100_000;
    private static readonly TimeSpan Least = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BackOff = TimeSpan.FromSeconds(5);

    public Task<byte[]> ReadAsync(long offset, long length, CancellationToken cancellationToken) =>
        Patience.TryAsync(async limit =>
        {
            using HttpRequestMessage request = new(HttpMethod.Get, archive);
            request.Headers.Range = new RangeHeaderValue(offset, offset + length - 1);
            using HttpResponseMessage response = await http.SendAsync(request, limit);
            if (response.StatusCode != HttpStatusCode.PartialContent)
                throw new HttpRequestException($"{archive} answered {(int)response.StatusCode} to a range request");
            byte[] bytes = await response.Content.ReadAsByteArrayAsync(limit);
            if (bytes.Length != length)
                throw new HttpRequestException($"{archive} sent {bytes.Length} bytes of {length}");
            return bytes;
        }, TimeFor(length), Attempts, BackOff, cancellationToken);

    public static TimeSpan TimeFor(long bytes) => Least + TimeSpan.FromSeconds((double)bytes / SlowestBytesPerSecond);
}
