extern alias ApiHost;

using Grpc.AspNetCore.Server;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Tiers;
using GSBC.ChurchApp.Platform.Contracts.Tiers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed class TierRules
{
    [Fact]
    public void EveryServedMethodDeclaresExactlyOneTierWithAKnownPermission()
    {
        using WebApplicationFactory<ApiHost::Program> api = new();
        PermissionCatalog permissions = api.Services.GetRequiredService<PermissionCatalog>();

        IEnumerable<(string Path, TierAttribute[] Tiers)> operations = api.Services
            .GetRequiredService<EndpointDataSource>().Endpoints
            .Select(endpoint => endpoint.Metadata.GetMetadata<GrpcMethodMetadata>()?.ServiceType)
            .OfType<Type>()
            .Where(type => type.Namespace?.StartsWith("GSBC.") == true)
            .Distinct()
            .SelectMany(type => type.GetInterfaces())
            .SelectMany(TierCatalog.Describe)
            .Select(operation => (operation.Path, operation.Tiers));

        List<string> violations = operations
            .Where(operation => operation.Tiers.Length != 1
                || (operation.Tiers[0] is RequiresAttribute requires && !permissions.ByNode.ContainsKey(requires.Node)))
            .Select(operation => operation.Path)
            .ToList();

        Assert.Empty(violations);
    }
}
