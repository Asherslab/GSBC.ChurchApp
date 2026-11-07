using Grpc.Health.V1;
using Grpc.Net.Client;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using static Grpc.Health.V1.HealthCheckResponse.Types;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class HealthProbeTests : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private WebApplication _api = null!;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)]);
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

        await _database.DropAsync();

        Assert.Equal(ServingStatus.Serving, (await health.CheckAsync(liveness, cancellationToken: cancellation)).Status);
        Assert.Equal(ServingStatus.NotServing, (await health.CheckAsync(readiness, cancellationToken: cancellation)).Status);
    }

    public async ValueTask DisposeAsync()
    {
        await _api.DisposeAsync();
        await _database.DisposeAsync();
    }
}
