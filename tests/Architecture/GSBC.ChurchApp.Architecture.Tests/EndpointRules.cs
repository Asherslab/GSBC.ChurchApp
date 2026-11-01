extern alias ApiHost;
extern alias GatewayHost;

using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Model;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed class EndpointRules
{
    private const string GrpcServicePrefix = "/gsbc.";
    private const string StaticFileFallback = "{*path:nonfile}";
    private const string GrpcUnimplementedMethod = "gRPC - Unimplemented method for ";

    private static readonly string[] ApprovedNonGrpcPaths = [];

    [Fact]
    public void EveryApiEndpointIsGrpcOrApproved()
    {
        using WebApplicationFactory<ApiHost::Program> api = new();
        Assert.Empty(Violations(api.Services));
    }

    [Fact]
    public void EveryGatewayEndpointIsGrpcStaticFilesOrApproved()
    {
        using WebApplicationFactory<GatewayHost::Program> gateway = new();
        Assert.Empty(Violations(gateway.Services));
    }

    private static List<string> Violations(IServiceProvider services)
    {
        return services.GetRequiredService<EndpointDataSource>().Endpoints
            .Where(endpoint => !IsAllowed(endpoint))
            .Select(endpoint => endpoint.DisplayName ?? "(unnamed endpoint)")
            .ToList();
    }

    private static bool IsAllowed(Endpoint endpoint)
    {
        if (endpoint.Metadata.GetMetadata<GrpcMethodMetadata>() is not null
            || endpoint.DisplayName?.StartsWith(GrpcUnimplementedMethod) == true)
            return true;

        if (endpoint.Metadata.GetMetadata<RouteModel>() is { } proxied)
            return IsGrpcProxyRoute(proxied);

        string? pattern = (endpoint as RouteEndpoint)?.RoutePattern.RawText;
        return pattern == StaticFileFallback || ApprovedNonGrpcPaths.Contains(pattern);
    }

    private static bool IsGrpcProxyRoute(RouteModel route) =>
        route.Config.Match.Path?.StartsWith(GrpcServicePrefix) == true
        && route.Config.Match.Headers?.Any(header =>
            header.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
            && header.Values?.All(value => value.StartsWith("application/grpc")) == true) == true;
}
