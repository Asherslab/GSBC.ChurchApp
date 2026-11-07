extern alias ApiHost;
extern alias GatewayHost;

using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Model;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed class EndpointRules
{
    private const string GrpcServicePrefix = "/gsbc.";
    private static readonly string[] StaticFileFallbacks = ["{*path:nonfile}", "{**path:file}"];
    private const string GrpcUnimplementedMethod = "gRPC - Unimplemented method for ";

    private static readonly string[] ApprovedNonGrpcPaths =
    [
        "/accounts/passkeys/creation-options",
        "/accounts/passkeys/register",
        "/accounts/passkeys/request-options",
        "/accounts/passkeys/sign-in",
    ];

    [Fact]
    public void EveryApiEndpointIsGrpcOrApproved()
    {
        using RoutesOnly<ApiHost::Program> api = new();
        Assert.Empty(Violations(api.Services));
    }

    [Fact]
    public void EveryGatewayEndpointIsGrpcStaticFilesOrApproved()
    {
        using RoutesOnly<GatewayHost::Program> gateway = new();
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

        if (endpoint.Metadata.GetMetadata<StaticAssetDescriptor>() is not null)
            return true;

        if (endpoint.Metadata.GetMetadata<RouteModel>() is { } proxied)
            return IsGrpcProxyRoute(proxied);

        string? pattern = (endpoint as RouteEndpoint)?.RoutePattern.RawText;
        return StaticFileFallbacks.Contains(pattern) || ApprovedNonGrpcPaths.Contains(pattern);
    }

    private static bool IsGrpcProxyRoute(RouteModel route) =>
        route.Config.Match.Path?.StartsWith(GrpcServicePrefix) == true
        && route.Config.Match.Headers?.Any(header =>
            header.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
            && header.Values?.All(value => value.StartsWith("application/grpc")) == true) == true;
}
