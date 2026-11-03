using System.Security.Claims;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Installs;
using GSBC.ChurchApp.Platform.Contracts.Tiers;
using Microsoft.AspNetCore.Http;

namespace GSBC.ChurchApp.Platform.Api.Tiers;

public sealed class TierInterceptor(TierCatalog catalog, IAccessEvaluator access, IInstallVerifier installs) : Interceptor
{
    private const string KubernetesHealthProbe = "/grpc.health.v1.Health/";
    private static readonly Status NotProved = new(StatusCode.Unauthenticated, "This phone hasn't proved it's the app.");

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        await AuthoriseAsync(request, context);
        return await continuation(request, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        await AuthoriseAsync(request, context);
        await continuation(request, responseStream, context);
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream, ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        await AuthoriseAsync(request: null, context);
        return await continuation(requestStream, context);
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        await AuthoriseAsync(request: null, context);
        await continuation(requestStream, responseStream, context);
    }

    private async Task AuthoriseAsync(object? request, ServerCallContext context)
    {
        if (context.Method.StartsWith(KubernetesHealthProbe, StringComparison.Ordinal))
            return;

        HttpContext http = context.GetHttpContext();
        Type? implementation = http.GetEndpoint()?.Metadata.GetMetadata<GrpcMethodMetadata>()?.ServiceType;
        TierAttribute[] tiers = implementation is null ? [] : catalog.TiersOf(implementation, context.Method);

        if (tiers.Length != 1)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "This method declares no single tier."));

        ClaimsPrincipal user = http.User;
        bool signedIn = user.Identity?.IsAuthenticated == true;

        switch (tiers[0])
        {
            case PublicAttribute:
                return;
            case SignedInAttribute when signedIn:
                return;
            case RequiresAttribute requires when signedIn:
                string? scopeId = (request as IScopedRequest)?.ScopeId;
                if (await access.IsAllowedAsync(user, requires.Node, scopeId, context.CancellationToken))
                    return;
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Not permitted."));
            case SignedInAttribute or RequiresAttribute:
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));
            case AppInstallAttribute:
                await ProveInstallAsync(http, context.CancellationToken);
                return;
            default:
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Unknown tier."));
        }
    }

    private async Task ProveInstallAsync(HttpContext http, CancellationToken cancellationToken)
    {
        InstallProof proof = InstallProofMiddleware.ProofOf(http)
            ?? throw new RpcException(NotProved, new Metadata { { InstallProofMiddleware.ProofRequiredTrailer, "required" } });
        Guid installId = await installs.VerifyAsync(proof, cancellationToken) ?? throw new RpcException(NotProved);
        CallingInstall.Set(http, installId);
    }
}
