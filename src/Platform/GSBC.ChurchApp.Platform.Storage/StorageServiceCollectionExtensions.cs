using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Platform.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddBlobStore(this IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(service => service.ServiceType == typeof(IBlobStore)))
            return services;

        IConfigurationSection section = configuration.GetSection(BlobStoreOptions.Section);
        services.Configure<BlobStoreOptions>(section);
        services.AddSingleton<IAmazonS3>(provider =>
        {
            BlobStoreOptions options = provider.GetRequiredService<IOptions<BlobStoreOptions>>().Value;
            return new AmazonS3Client(
                new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
                new AmazonS3Config
                {
                    ServiceURL = options.ServiceUrl,
                    AuthenticationRegion = options.Region,
                    ForcePathStyle = true,
                    RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                    ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                });
        });
        services.TryAddSingleton<IBlobStore, S3BlobStore>();
        if (section.GetValue<bool>(nameof(BlobStoreOptions.CreateBuckets)))
            services.AddHostedService<BucketCreator>();
        return services;
    }
}
