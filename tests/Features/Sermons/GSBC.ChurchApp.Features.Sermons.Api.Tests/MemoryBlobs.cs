using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Storage;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

internal sealed class MemoryBlobs : IBlobStore
{
    public Dictionary<(BlobBucket, string), (byte[] Bytes, BlobHeaders Headers)> Objects { get; } = [];

    public Task<Uri> PresignUploadAsync(BlobBucket bucket, string key, string contentType, TimeSpan validFor) => Task.FromResult(new Uri($"https://upload.example/{key}"));

    public Task<Uri> PresignDownloadAsync(BlobBucket bucket, string key, TimeSpan validFor) => Task.FromResult(new Uri($"https://private.example/{key}"));

    public Uri PublicUrlOf(string key) => new($"https://media.example/{key}");

    public Task<BlobInfo?> FindAsync(BlobBucket bucket, string key, CancellationToken cancellationToken) =>
        Task.FromResult(Objects.TryGetValue((bucket, key), out var found) ? new BlobInfo(found.Bytes.Length, found.Headers.ContentType) : null);

    public Task<Stream> OpenAsync(BlobBucket bucket, string key, CancellationToken cancellationToken) => Task.FromResult<Stream>(new MemoryStream(Objects[(bucket, key)].Bytes));

    public async Task PutAsync(BlobBucket bucket, string key, Stream content, BlobHeaders headers, CancellationToken cancellationToken)
    {
        using MemoryStream copy = new();
        await content.CopyToAsync(copy, cancellationToken);
        Objects[(bucket, key)] = (copy.ToArray(), headers);
    }

    public Task CopyAsync(BlobBucket fromBucket, string fromKey, BlobBucket toBucket, string toKey, BlobHeaders headers, CancellationToken cancellationToken)
    {
        Objects[(toBucket, toKey)] = (Objects[(fromBucket, fromKey)].Bytes, headers);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(BlobBucket bucket, string key, CancellationToken cancellationToken)
    {
        Objects.Remove((bucket, key));
        return Task.CompletedTask;
    }
}

internal sealed class AnyMedia : IMediaReferences, IMediaSystemFolders
{
    private readonly Dictionary<string, MediaRef> _kept = [];

    public List<KeptFile> Kept { get; } = [];
    public Dictionary<string, List<string>> Uses { get; } = [];

    public Task FileAsync(MediaSystemArea area, string? scopeId, string scopeName, IReadOnlyCollection<MediaRef> media, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<MediaRef> KeepAsync(MediaSystemArea area, KeptFile file, CancellationToken cancellationToken)
    {
        string key = $"{Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(file.Bytes))}.{file.Extension}";
        if (!_kept.TryGetValue(key, out MediaRef? kept))
        {
            Kept.Add(file);
            kept = _kept[key] = new MediaRef { Id = Guid.CreateVersion7().ToString(), Key = key, ContentType = file.ContentType, Width = file.Width, Height = file.Height };
        }
        return Task.FromResult(kept);
    }

    public List<StoredFile> Stored { get; } = [];

    public Task<MediaRef> KeepStoredAsync(MediaSystemArea area, StoredFile file, CancellationToken cancellationToken)
    {
        if (!_kept.TryGetValue(file.Sha256, out MediaRef? kept))
        {
            Stored.Add(file);
            kept = _kept[file.Sha256] = new MediaRef { Id = Guid.CreateVersion7().ToString(), Key = file.Key, ContentType = file.ContentType, Bytes = file.Bytes };
        }
        return Task.FromResult(kept);
    }

    public Dictionary<string, string> Named { get; } = [];

    public Task NameAsync(MediaSystemArea area, string mediaId, string fileName, CancellationToken cancellationToken)
    {
        Named[mediaId] = fileName;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaRef>>(claimed.ToList());

    public Task UseAsync(string owner, IReadOnlyCollection<MediaRef> verified, CancellationToken cancellationToken)
    {
        Uses[owner] = verified.Select(media => media.Id).ToList();
        return Task.CompletedTask;
    }
}
