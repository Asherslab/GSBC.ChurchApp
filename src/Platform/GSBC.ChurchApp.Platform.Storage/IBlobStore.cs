namespace GSBC.ChurchApp.Platform.Storage;

public enum BlobBucket
{
    Public,
    Private,
}

public sealed record BlobInfo(long Bytes, string? ContentType);

public interface IBlobStore
{
    Task<Uri> PresignUploadAsync(BlobBucket bucket, string key, string contentType, TimeSpan validFor);

    Task<Uri> PresignDownloadAsync(BlobBucket bucket, string key, TimeSpan validFor);

    Uri PublicUrlOf(string key);

    Task<BlobInfo?> FindAsync(BlobBucket bucket, string key, CancellationToken cancellationToken);

    Task<Stream> OpenAsync(BlobBucket bucket, string key, CancellationToken cancellationToken);

    Task CopyAsync(BlobBucket fromBucket, string fromKey, BlobBucket toBucket, string toKey, BlobHeaders headers, CancellationToken cancellationToken);

    Task DeleteAsync(BlobBucket bucket, string key, CancellationToken cancellationToken);
}

public sealed record BlobHeaders(string ContentType, string CacheControl);
