using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Platform.Storage;

internal sealed class S3BlobStore(IAmazonS3 s3, S3Presigner presigner, IOptions<BlobStoreOptions> options) : IBlobStore
{
    private BlobStoreOptions Options => options.Value;

    public async Task<Uri> PresignUploadAsync(BlobBucket bucket, string key, string contentType, TimeSpan validFor) =>
        new(await presigner.Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = NameOf(bucket),
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow + validFor,
            Protocol = Protocol,
        }));

    public async Task<Uri> PresignDownloadAsync(BlobBucket bucket, string key, TimeSpan validFor) =>
        new(await presigner.Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = NameOf(bucket),
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow + validFor,
            Protocol = Protocol,
        }));

    public Uri PublicUrlOf(string key) => new($"{Options.PublicBaseUrl.TrimEnd('/')}/{key}");

    public async Task<BlobInfo?> FindAsync(BlobBucket bucket, string key, CancellationToken cancellationToken)
    {
        try
        {
            GetObjectMetadataResponse metadata = await s3.GetObjectMetadataAsync(NameOf(bucket), key, cancellationToken);
            return new BlobInfo(metadata.ContentLength, metadata.Headers.ContentType);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Stream> OpenAsync(BlobBucket bucket, string key, CancellationToken cancellationToken)
    {
        GetObjectResponse response = await s3.GetObjectAsync(NameOf(bucket), key, cancellationToken);
        return response.ResponseStream;
    }

    public Task CopyAsync(BlobBucket fromBucket, string fromKey, BlobBucket toBucket, string toKey, BlobHeaders headers, CancellationToken cancellationToken)
    {
        CopyObjectRequest request = new()
        {
            SourceBucket = NameOf(fromBucket),
            SourceKey = fromKey,
            DestinationBucket = NameOf(toBucket),
            DestinationKey = toKey,
            MetadataDirective = S3MetadataDirective.REPLACE,
            ContentType = headers.ContentType,
        };
        request.Headers.CacheControl = headers.CacheControl;
        return s3.CopyObjectAsync(request, cancellationToken);
    }

    public Task DeleteAsync(BlobBucket bucket, string key, CancellationToken cancellationToken) =>
        s3.DeleteObjectAsync(NameOf(bucket), key, cancellationToken);

    private Protocol Protocol => (Options.PublicServiceUrl ?? Options.ServiceUrl).StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? Protocol.HTTP : Protocol.HTTPS;

    private string NameOf(BlobBucket bucket) => bucket == BlobBucket.Public ? Options.PublicBucket : Options.PrivateBucket;
}
