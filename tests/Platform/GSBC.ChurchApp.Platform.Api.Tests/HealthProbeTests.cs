using Grpc.Health.V1;
using Grpc.Net.Client;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using static Grpc.Health.V1.HealthCheckResponse.Types;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class HealthProbeTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private WebApplication _api = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _postgres.GetConnectionString())]);
        builder.AddPlatformApi();
        _api = builder.Build();
        _api.MapPlatformApi();
        await _api.StartAsync();
    }

    [Fact]
    public async Task KubernetesProbesNeedNoTierAndOnlyReadinessFollowsTheDatabase()
    {
        TestServer server = _api.GetTestServer();
        using GrpcChannel channel = GrpcChannel.ForAddress(server.BaseAddress, new GrpcChannelOptions { HttpHandler = server.CreateHandler() });
        Health.HealthClient health = new(channel);
        HealthCheckRequest liveness = new();
        HealthCheckRequest readiness = new() { Service = PlatformApiExtensions.ReadinessService };
        CancellationToken cancellation = TestContext.Current.CancellationToken;

        Assert.Equal(ServingStatus.Serving, (await health.CheckAsync(liveness, cancellationToken: cancellation)).Status);
        Assert.Equal(ServingStatus.Serving, (await health.CheckAsync(readiness, cancellationToken: cancellation)).Status);

        await _postgres.StopAsync(cancellation);

        Assert.Equal(ServingStatus.Serving, (await health.CheckAsync(liveness, cancellationToken: cancellation)).Status);
        Assert.Equal(ServingStatus.NotServing, (await health.CheckAsync(readiness, cancellationToken: cancellation)).Status);
    }

    public async ValueTask DisposeAsync()
    {
        await _api.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
