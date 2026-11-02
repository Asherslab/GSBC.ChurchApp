using Amazon.S3;
using Amazon.S3.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Platform.Storage;

internal sealed class BucketCreator(IAmazonS3 s3, IOptions<BlobStoreOptions> options, ILogger<BucketCreator> logger) : BackgroundService
{
    private const int Attempts = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (int attempt = 1; attempt <= Attempts; attempt++)
        {
            try
            {
                foreach (string bucket in new[] { options.Value.PublicBucket, options.Value.PrivateBucket })
                {
                    if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3, bucket))
                        await s3.PutBucketAsync(bucket, stoppingToken);
                }
                return;
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested && attempt < Attempts)
            {
                logger.LogDebug(exception, "The blob store isn't ready yet");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
