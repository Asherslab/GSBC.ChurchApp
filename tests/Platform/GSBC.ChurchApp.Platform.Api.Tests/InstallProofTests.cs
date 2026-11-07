using Grpc.Core.Interceptors;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Installs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Grpc.Health.V1.HealthCheckResponse.Types;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class InstallProofTests : IAsyncLifetime
{
    private WebApplication _api = null!;

    public async ValueTask InitializeAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddPlatformApi();
        foreach (ServiceDescriptor appWork in builder.Services.Where(IsTheAppsBackgroundWork).ToList())
            builder.Services.Remove(appWork);
        _api = builder.Build();
        _api.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
        _api.MapPlatformApi();
        await _api.StartAsync();
    }

    [Fact]
    public async Task ASignedCallStillReachesItsMethodWithItsMessage()
    {
        TestServer server = _api.GetTestServer();
        using GrpcChannel channel = GrpcChannel.ForAddress(server.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, server.CreateHandler()),
        });
        Health.HealthClient health = new(channel.Intercept(metadata =>
        {
            metadata.Add(InstallProofMiddleware.KeyHeader, "a-phone");
            metadata.Add(InstallProofMiddleware.AssertionHeader, Convert.ToBase64String([1, 2, 3]));
            return metadata;
        }));

        HealthCheckResponse response = await health.CheckAsync(new HealthCheckRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ServingStatus.Serving, response.Status);
    }

    private static bool IsTheAppsBackgroundWork(ServiceDescriptor service) =>
        service.ServiceType == typeof(IHostedService)
        && service.ImplementationType?.Namespace?.StartsWith("Microsoft.", StringComparison.Ordinal) != true;

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();
}
