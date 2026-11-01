using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Platform.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddBlobStore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BlobStoreOptions>(configuration.GetSection(BlobStoreOptions.Section));
        services.AddSingleton<IAmazonS3>(provider =>
        {
            BlobStoreOptions options = provider.GetRequiredService<IOptions<BlobStoreOptions>>().Value;
            return new AmazonS3Client(
                new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
                new AmazonS3Config { ServiceURL = options.ServiceUrl, ForcePathStyle = true });
        });
        services.AddSingleton<IBlobStore, S3BlobStore>();
        return services;
    }
}
