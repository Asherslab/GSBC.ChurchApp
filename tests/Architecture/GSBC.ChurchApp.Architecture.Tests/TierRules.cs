extern alias ApiHost;

using Grpc.AspNetCore.Server;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Installs;
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

        List<string> violations = ServedOperations(api)
            .Where(operation => operation.Tiers.Length != 1
                || (operation.Tiers[0] is RequiresAttribute requires && !permissions.ByNode.ContainsKey(requires.Node)))
            .Select(operation => operation.Path)
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void ThePhoneIsToldToSignEveryServedAppInstallMethodAndNothingElse()
    {
        using WebApplicationFactory<ApiHost::Program> api = new();

        IEnumerable<string> appInstallMethods = ServedOperations(api)
            .Where(operation => operation.Tiers is [AppInstallAttribute])
            .Select(operation => operation.Path)
            .Distinct()
            .ToList();

        Assert.Equal(appInstallMethods.Order(StringComparer.Ordinal), api.Services.GetRequiredService<InstallProofMethods>().Paths);
        Assert.Contains("/gsbc.notifications.Notifications/RegisterDevice", appInstallMethods);
    }

    private static IEnumerable<(string Path, TierAttribute[] Tiers)> ServedOperations(WebApplicationFactory<ApiHost::Program> api) =>
        api.Services
            .GetRequiredService<EndpointDataSource>().Endpoints
            .Select(endpoint => endpoint.Metadata.GetMetadata<GrpcMethodMetadata>()?.ServiceType)
            .OfType<Type>()
            .Where(type => type.Namespace?.StartsWith("GSBC.") == true)
            .Distinct()
            .SelectMany(type => type.GetInterfaces())
            .SelectMany(TierCatalog.Describe)
            .Select(operation => (operation.Path, operation.Tiers));
}
