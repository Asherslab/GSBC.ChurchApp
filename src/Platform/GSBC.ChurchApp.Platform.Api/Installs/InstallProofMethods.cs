using Grpc.AspNetCore.Server;
using GSBC.ChurchApp.Platform.Api.Tiers;
using GSBC.ChurchApp.Platform.Contracts.Tiers;
using Microsoft.AspNetCore.Routing;

namespace GSBC.ChurchApp.Platform.Api.Installs;

public sealed class InstallProofMethods(EndpointDataSource endpoints, TierCatalog catalog)
{
    private IReadOnlyList<string>? _paths;

    public IReadOnlyList<string> Paths => _paths ??= endpoints.Endpoints
        .Select(endpoint => endpoint.Metadata.GetMetadata<GrpcMethodMetadata>())
        .OfType<GrpcMethodMetadata>()
        .Select(grpc => (grpc.Method.FullName, Tiers: catalog.TiersOf(grpc.ServiceType, grpc.Method.FullName)))
        .Where(method => method.Tiers is [AppInstallAttribute])
        .Select(method => method.FullName)
        .Distinct()
        .Order(StringComparer.Ordinal)
        .ToList();
}
