using System.Security.Cryptography;
using System.Text.Json;

namespace GSBC.ChurchApp.Hosts.AppHost;

internal static class DevMediaStore
{
    private const int Port = 5310;
    private const string TunnelHostname = "dev-media.baptist.com.au";
    private const string PublicBucket = "gsbc-media";
    private const string PrivateBucket = "gsbc-private";
    private const string ConfigFolder = "/etc/gsbc";

    public static void AddDevMediaStore(this IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> api, DevTunnel? tunnel, int portOffset)
    {
        string accessKey = RandomNumberGenerator.GetHexString(20, lowercase: true);
        string secretKey = RandomNumberGenerator.GetHexString(40, lowercase: true);
        int port = Port + portOffset;

        IResourceBuilder<ContainerResource> store = builder
            .AddContainer("media", "chrislusf/seaweedfs", "4.50")
            .WithArgs("mini", "-dir=/data", $"-s3.config={ConfigFolder}/s3.json", "-s3.allowedOrigins=*")
            .WithContainerFiles(ConfigFolder, [new ContainerFile { Name = "s3.json", Contents = Identities(accessKey, secretKey) }])
            .WithVolume(portOffset == 0 ? "gsbc-media-data" : $"gsbc-media-data-{portOffset}", "/data")
            .WithHttpEndpoint(port: port, targetPort: 8333, name: "s3", isProxied: false);

        string serviceUrl = $"http://localhost:{port}";
        string publicServiceUrl = tunnel?.Serves(TunnelHostname) == true ? $"https://{TunnelHostname}" : serviceUrl;
        api
            .WithEnvironment("Storage__Blobs__ServiceUrl", serviceUrl)
            .WithEnvironment("Storage__Blobs__PublicServiceUrl", publicServiceUrl)
            .WithEnvironment("Storage__Blobs__AccessKeyId", accessKey)
            .WithEnvironment("Storage__Blobs__SecretAccessKey", secretKey)
            .WithEnvironment("Storage__Blobs__PublicBucket", PublicBucket)
            .WithEnvironment("Storage__Blobs__PrivateBucket", PrivateBucket)
            .WithEnvironment("Storage__Blobs__PublicBaseUrl", $"{publicServiceUrl}/{PublicBucket}")
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
