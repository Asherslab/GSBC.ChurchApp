using System.Net;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace GSBC.ChurchApp.Testing;

internal sealed class SharedS3
{
    public const string AccessKey = "test-access-key";
    public const string SecretKey = "test-secret-key";
    public const string PublicBucketPrefix = "media-";
    public const string PrivateBucketPrefix = "private-";
    private const int Port = 8333;
    private static readonly Lazy<Task<SharedS3>> Shared = new(StartAsync);

    private readonly IContainer _container;

    private SharedS3(IContainer container) => _container = container;

    public string ServiceUrl => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(Port)}";

    public static Task<SharedS3> InstanceAsync() => Shared.Value;

    private static async Task<SharedS3> StartAsync()
    {
        IContainer container = new ContainerBuilder("chrislusf/seaweedfs:4.50")
            .WithCommand("mini", "-dir=/data", "-volume.max=1000", "-s3.config=/etc/gsbc/s3.json")
            .WithResourceMapping(Encoding.UTF8.GetBytes($$"""
                {"identities":[
                  {"name":"api","credentials":[{"accessKey":"{{AccessKey}}","secretKey":"{{SecretKey}}"}],"actions":["Admin","Read","Write","List"]},
                  {"name":"anonymous","actions":["Read:{{PublicBucketPrefix}}*"]}
                ]}
                """), "/etc/gsbc/s3.json")
            .WithLabel("gsbc.churchapp", "tests")
            .WithPortBinding(Port, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request.ForPort(Port).ForPath("/").ForStatusCode(HttpStatusCode.OK).ForStatusCode(HttpStatusCode.Forbidden))
                .UntilMessageIsLogged(@"noticed \.filer"))
            .Build();
        await container.StartAsync();
        return new SharedS3(container);
    }
}
