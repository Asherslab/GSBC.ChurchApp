using System.Security.Cryptography;
using System.Text.Json;

namespace GSBC.ChurchApp.Hosts.AppHost;

internal static class DevMediaStore
{
    public const int Port = 5310;
    public const string PublicBucket = "gsbc-media";
    public const string PrivateBucket = "gsbc-private";
    private const string ConfigFolder = "/etc/gsbc";

    public static void AddDevMediaStore(this IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> api)
    {
        string accessKey = RandomNumberGenerator.GetHexString(20, lowercase: true);
        string secretKey = RandomNumberGenerator.GetHexString(40, lowercase: true);

        IResourceBuilder<ContainerResource> store = builder
            .AddContainer("media", "chrislusf/seaweedfs", "4.50")
            .WithArgs("mini", "-dir=/data", $"-s3.config={ConfigFolder}/s3.json", "-s3.allowedOrigins=*")
            .WithContainerFiles(ConfigFolder, [new ContainerFile { Name = "s3.json", Contents = Identities(accessKey, secretKey) }])
            .WithVolume("gsbc-media-data", "/data")
            .WithHttpEndpoint(port: Port, targetPort: 8333, name: "s3", isProxied: false);

        string serviceUrl = $"http://localhost:{Port}";
        api
            .WithEnvironment("Storage__Blobs__ServiceUrl", serviceUrl)
            .WithEnvironment("Storage__Blobs__AccessKeyId", accessKey)
            .WithEnvironment("Storage__Blobs__SecretAccessKey", secretKey)
            .WithEnvironment("Storage__Blobs__PublicBucket", PublicBucket)
            .WithEnvironment("Storage__Blobs__PrivateBucket", PrivateBucket)
            .WithEnvironment("Storage__Blobs__PublicBaseUrl", $"{serviceUrl}/{PublicBucket}")
            .WithEnvironment("Storage__Blobs__CreateBuckets", "true")
            .WaitFor(store);
    }

    private static string Identities(string accessKey, string secretKey) => JsonSerializer.Serialize(new
    {
        identities = new object[]
        {
            new
            {
                name = "api",
                credentials = new[] { new { accessKey, secretKey } },
                actions = new[] { "Admin", "Read", "Write", "List", "Tagging" },
            },
            new { name = "anonymous", actions = new[] { $"Read:{PublicBucket}" } },
        },
    });
}
