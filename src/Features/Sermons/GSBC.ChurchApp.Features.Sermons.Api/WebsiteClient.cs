using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed record FetchedFeed(bool Changed, byte[] Body, string? ETag, string? LastModified);

internal sealed class WebsiteClient(HttpClient http, IOptions<WebsiteOptions> options)
{
    public const string UserAgent = "GSBC-ChurchApp/1.0 (+https://app.baptist.com.au)";
    private const int HeadBytes = 64 * 1024;

    private WebsiteOptions Options => options.Value;

    public Uri Absolute(string path) => new(new Uri(Options.BaseUrl), path);

    public Task<FetchedFeed> FeedAsync(string? etag, string? lastModified, CancellationToken cancellationToken) =>
        IfChangedAsync(Absolute(Options.FeedPath), etag, lastModified, cancellationToken);

    public async Task<FetchedFeed> IfChangedAsync(Uri url, string? etag, string? lastModified, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        if (etag is not null && EntityTagHeaderValue.TryParse(etag, out EntityTagHeaderValue? tag))
            request.Headers.IfNoneMatch.Add(tag);
        if (lastModified is not null && DateTimeOffset.TryParse(lastModified, out DateTimeOffset modified))
            request.Headers.IfModifiedSince = modified;
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotModified)
            return new FetchedFeed(false, [], etag, lastModified);
        response.EnsureSuccessStatusCode();
        return new FetchedFeed(true,
            await response.Content.ReadAsByteArrayAsync(cancellationToken),
            response.Headers.ETag?.ToString(),
            response.Content.Headers.LastModified?.ToString("R"));
    }

    public async Task<string> PageAsync(string path, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, Absolute(path));
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<byte[]> BytesAsync(string url, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<Stream> StreamAsync(string url, CancellationToken cancellationToken)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url);
        HttpResponseMessage response = await http.SendAsync(Signed(request), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<byte[]> HeadOfAsync(string url, int skip, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(skip, skip + HeadBytes - 1);
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        byte[] body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return response.StatusCode == HttpStatusCode.PartialContent || skip == 0 ? body.AsSpan(0, Math.Min(body.Length, HeadBytes)).ToArray() : body.AsSpan(skip, Math.Min(body.Length - skip, HeadBytes)).ToArray();
    }

    private Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        http.SendAsync(Signed(request), cancellationToken);

    private static HttpRequestMessage Signed(HttpRequestMessage request)
    {
        request.Headers.UserAgent.ParseAdd(UserAgent);
        return request;
    }
}
