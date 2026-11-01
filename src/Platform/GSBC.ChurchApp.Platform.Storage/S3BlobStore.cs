using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Platform.Storage;

internal sealed class S3BlobStore(IAmazonS3 s3, IOptions<BlobStoreOptions> options) : IBlobStore
{
    private string Bucket => options.Value.Bucket;

    public Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken) =>
        s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        }, cancellationToken);

    public async Task<Stream> OpenAsync(string key, CancellationToken cancellationToken)
    {
        GetObjectResponse response = await s3.GetObjectAsync(Bucket, key, cancellationToken);
        return response.ResponseStream;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        s3.DeleteObjectAsync(Bucket, key, cancellationToken);
}
