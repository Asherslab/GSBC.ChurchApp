using System.Net;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Tests;

internal sealed class FakeWebsite : HttpMessageHandler
{
    public const string BaseUrl = "https://church.example";

    private readonly Dictionary<string, Func<HttpResponseMessage>> _routes = new(StringComparer.Ordinal);

    public List<string> Requested { get; } = [];

    public void ServeText(string url, string body) =>
        _routes[url] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/html") };

    public void ServeBytes(string url, byte[] body) =>
        _routes[url] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) };

    public void Fail(string url, HttpStatusCode status) => _routes[url] = () => new HttpResponseMessage(status);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string url = request.RequestUri!.GetLeftPart(UriPartial.Path) + request.RequestUri.Query;
        Requested.Add(url);
        return Task.FromResult(_routes.TryGetValue(url, out var respond) ? respond() : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

internal sealed class MemoryBlobs : IBlobStore
{
    public Dictionary<(BlobBucket, string), byte[]> Objects { get; } = [];

    public Task<Uri> PresignUploadAsync(BlobBucket bucket, string key, string contentType, TimeSpan validFor) => Task.FromResult(new Uri($"https://upload.example/{key}"));

    public Task<Uri> PresignDownloadAsync(BlobBucket bucket, string key, TimeSpan validFor) => Task.FromResult(new Uri($"https://private.example/{key}"));

    public Uri PublicUrlOf(string key) => new($"https://media.example/{key}");

    public Task<BlobInfo?> FindAsync(BlobBucket bucket, string key, CancellationToken cancellationToken) =>
        Task.FromResult(Objects.TryGetValue((bucket, key), out byte[]? found) ? new BlobInfo(found.Length, "application/octet-stream") : null);

    public Task<Stream> OpenAsync(BlobBucket bucket, string key, CancellationToken cancellationToken) => Task.FromResult<Stream>(new MemoryStream(Objects[(bucket, key)]));

    public async Task PutAsync(BlobBucket bucket, string key, Stream content, BlobHeaders headers, CancellationToken cancellationToken)
    {
        using MemoryStream copy = new();
        await content.CopyToAsync(copy, cancellationToken);
        Objects[(bucket, key)] = copy.ToArray();
    }

    public Task CopyAsync(BlobBucket fromBucket, string fromKey, BlobBucket toBucket, string toKey, BlobHeaders headers, CancellationToken cancellationToken)
    {
        Objects[(toBucket, toKey)] = Objects[(fromBucket, fromKey)];
        return Task.CompletedTask;
    }

    public Task DeleteAsync(BlobBucket bucket, string key, CancellationToken cancellationToken)
    {
        Objects.Remove((bucket, key));
        return Task.CompletedTask;
    }
}

internal sealed class KeptMedia : IMediaReferences, IMediaSystemFolders
{
    private readonly Dictionary<string, MediaRef> _byContent = [];

    public int Kept => _byContent.Count;

    public Task FileAsync(MediaSystemArea area, string? scopeId, string scopeName, IReadOnlyCollection<MediaRef> media, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<MediaRef> KeepAsync(MediaSystemArea area, KeptFile file, CancellationToken cancellationToken) =>
        Task.FromResult(Keep(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(file.Bytes)), $"{file.Extension}", file.ContentType, file.Bytes.Length));

    public Task<MediaRef> KeepStoredAsync(MediaSystemArea area, StoredFile file, CancellationToken cancellationToken) =>
        Task.FromResult(Keep(file.Sha256, Path.GetExtension(file.Key).TrimStart('.'), file.ContentType, file.Bytes));

    public Task NameAsync(MediaSystemArea area, string mediaId, string fileName, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaRef>>(claimed.ToList());

    public Task UseAsync(string owner, IReadOnlyCollection<MediaRef> verified, CancellationToken cancellationToken) => Task.CompletedTask;

    private MediaRef Keep(string sha256, string extension, string contentType, long bytes)
    {
        if (!_byContent.TryGetValue(sha256, out MediaRef? kept))
            kept = _byContent[sha256] = new MediaRef { Id = Guid.CreateVersion7().ToString(), Key = $"{sha256}.{extension}", ContentType = contentType, Bytes = bytes };
        return new MediaRef { Id = kept.Id, Key = kept.Key, ContentType = kept.ContentType, Bytes = kept.Bytes };
    }
}
