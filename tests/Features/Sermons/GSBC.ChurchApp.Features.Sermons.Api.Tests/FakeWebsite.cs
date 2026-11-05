using System.Net;
using System.Net.Http.Headers;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

internal sealed class FakeWebsite : HttpMessageHandler
{
    public const string BaseUrl = "https://church.example";

    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new(StringComparer.Ordinal);

    public List<HttpRequestMessage> Requests { get; } = [];

    public string FeedETag { get; set; } = "W/\"one\"";

    public void Serve(string url, Func<HttpRequestMessage, HttpResponseMessage> respond) => _routes[url] = respond;

    public void ServeText(string url, string body, string type = "text/html") =>
        Serve(url, _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, type) });

    public void ServeBytes(string url, byte[] body, string? etag = null) =>
        Serve(url, request =>
        {
            if (etag is not null && request.Headers.IfNoneMatch.Any(tag => tag.ToString() == etag))
                return new HttpResponseMessage(HttpStatusCode.NotModified);
            byte[] slice = body;
            HttpStatusCode status = HttpStatusCode.OK;
            if (request.Headers.Range?.Ranges.FirstOrDefault() is { From: { } from } range)
            {
                long to = Math.Min(range.To ?? body.Length - 1, body.Length - 1);
                slice = body[(int)from..(int)(to + 1)];
                status = HttpStatusCode.PartialContent;
            }
            HttpResponseMessage response = new(status) { Content = new ByteArrayContent(slice) };
            if (etag is not null)
                response.Headers.ETag = EntityTagHeaderValue.Parse(etag);
            return response;
        });

    public void Fail(string url, HttpStatusCode status) => Serve(url, _ => new HttpResponseMessage(status));

    public void ServeFeed(string xml) =>
        Serve($"{BaseUrl}/podcasts/media.rss", request =>
        {
            if (request.Headers.IfNoneMatch.Any(tag => tag.ToString() == FeedETag))
                return new HttpResponseMessage(HttpStatusCode.NotModified);
            HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new StringContent(xml, System.Text.Encoding.UTF8, "application/rss+xml") };
            response.Headers.ETag = EntityTagHeaderValue.Parse(FeedETag);
            return response;
        });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        string url = request.RequestUri!.GetLeftPart(UriPartial.Path) + request.RequestUri.Query;
        return Task.FromResult(_routes.TryGetValue(url, out var respond)
            ? respond(request)
            : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
