using System.Net;
using System.Net.Sockets;
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
    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(2);
    private static readonly Lazy<Task<SharedS3>> Shared = new(StartAsync);

    private static readonly string ServerThatStopsWhenUnused = $$"""
        /entrypoint.sh mini -dir=/data -volume.max=1000 -s3.config=/etc/gsbc/s3.json &
        server=$!
        held=0
        idle=0
        while sleep 1 && kill -0 $server 2>/dev/null; do
          if [ "$(awk '$2 ~ /:{{Port:X4}}$/ && $4 == "01"' /proc/net/tcp /proc/net/tcp6 2>/dev/null | wc -l)" -gt 0 ]; then held=1; idle=0; else idle=$((idle + 1)); fi
          if [ $held = 1 ] && [ $idle -ge 30 ] || [ $idle -ge 300 ]; then kill $server; wait $server; exit 0; fi
        done
        """;

    private readonly TcpClient _keepsTheServerRunning;

    private SharedS3(TcpClient keepsTheServerRunning) => _keepsTheServerRunning = keepsTheServerRunning;

    public string ServiceUrl => $"http://{_keepsTheServerRunning.Client.RemoteEndPoint}";

    public static Task<SharedS3> InstanceAsync() => Shared.Value;

    private static async Task<SharedS3> StartAsync()
    {
        using CancellationTokenSource timeout = new(StartTimeout);
        IContainer container = new ContainerBuilder("chrislusf/seaweedfs:4.50")
            .WithCleanUp(false)
            .WithAutoRemove(true)
            .WithEntrypoint("sh", "-c", ServerThatStopsWhenUnused)
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
        await container.StartAsync(timeout.Token);
        TcpClient keepsTheServerRunning = new();
        await keepsTheServerRunning.ConnectAsync(container.Hostname, container.GetMappedPublicPort(Port), timeout.Token);
        return new SharedS3(keepsTheServerRunning);
    }
}
