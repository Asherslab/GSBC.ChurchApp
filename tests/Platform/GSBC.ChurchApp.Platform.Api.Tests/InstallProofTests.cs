using System.Net;
using Grpc.Core.Interceptors;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Installs;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using static Grpc.Health.V1.HealthCheckResponse.Types;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class InstallProofTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private WebApplication _api = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http1));
        builder.Configuration.AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _postgres.GetConnectionString())]);
        builder.AddPlatformApi();
        _api = builder.Build();
        _api.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
        _api.MapPlatformApi();
        await _api.StartAsync();
    }

    [Fact]
    public async Task ASignedCallStillReachesItsMethodWithItsMessage()
    {
        string address = _api.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses
            .First(address => address.StartsWith("http://127.0.0.1:", StringComparison.Ordinal));
        using GrpcChannel channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()),
            HttpVersion = HttpVersion.Version11,
            HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact,
        });
        Health.HealthClient health = new(channel.Intercept(metadata =>
        {
            metadata.Add(InstallProofMiddleware.KeyHeader, "a-phone");
            metadata.Add(InstallProofMiddleware.AssertionHeader, Convert.ToBase64String([1, 2, 3]));
            return metadata;
        }));

        HealthCheckResponse response = await health.CheckAsync(
            new HealthCheckRequest { Service = PlatformApiExtensions.ReadinessService },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ServingStatus.Serving, response.Status);
    }

    public async ValueTask DisposeAsync()
    {
        await _api.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
