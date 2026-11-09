using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api;

internal sealed class PageGate
{
    public DateTimeOffset LastPage { get; set; } = DateTimeOffset.MinValue;
}

internal sealed class ArchiveClient(HttpClient http, IOptions<ImportOptions> options, PageGate gate, TimeProvider time)
{
    private const string UserAgent = "GSBC-ChurchApp/1.0 (+https://app.baptist.com.au; importing the church's sermons)";

    public string WebsiteUrl => options.Value.WebsiteUrl.TrimEnd('/');

    public async Task<string> PageAsync(string path, CancellationToken cancellationToken)
    {
        TimeSpan wait = gate.LastPage + options.Value.PageDelay - time.GetUtcNow();
        if (wait > TimeSpan.Zero)
            await Task.Delay(wait, time, cancellationToken);
        try
        {
            using HttpResponseMessage response = await SendAsync(new Uri(WebsiteUrl + path), HttpCompletionOption.ResponseContentRead, cancellationToken);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        finally
        {
            gate.LastPage = time.GetUtcNow();
        }
    }

    public async Task<byte[]> BytesAsync(string url, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendAsync(new Uri(url), HttpCompletionOption.ResponseContentRead, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<(Stream Body, long? Length, string? ContentType)> OpenAsync(string url, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await SendAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return (await response.Content.ReadAsStreamAsync(cancellationToken), response.Content.Headers.ContentLength, response.Content.Headers.ContentType?.MediaType);
    }

    private async Task<HttpResponseMessage> SendAsync(Uri url, HttpCompletionOption completion, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        HttpResponseMessage response = await http.SendAsync(request, completion, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            throw new HttpRequestException($"The website answered {(int)response.StatusCode}.", null, response.StatusCode);
        }
        return response;
    }
}
