using Grpc.Core;
using Grpc.Core.Interceptors;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class ServerErrorInterceptor(IServiceScopeFactory scopes, ILogger<ServerErrorInterceptor> logger) : Interceptor
{
    private const string HealthProbes = "/grpc.health.v1.Health/";

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (Exception exception) when (Observe(exception, context))
        {
            throw;
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (Exception exception) when (Observe(exception, context))
        {
            throw;
        }
    }

    private bool Observe(Exception exception, ServerCallContext context)
    {
        if (context.Method.StartsWith(HealthProbes, StringComparison.Ordinal) || context.CancellationToken.IsCancellationRequested)
            return false;

        (StatusCode status, Exception? unexpected) = exception switch
        {
            RpcException { StatusCode: StatusCode.Cancelled } => (StatusCode.OK, null),
            RpcException rpc => (rpc.StatusCode, null),
            OperationCanceledException => (StatusCode.OK, null),
            _ => (StatusCode.Unknown, exception),
        };
        if (FailedCalls.IsAFailure(status))
            _ = RecordAsync(context.Method, status, unexpected);
        return false;
    }

    private async Task RecordAsync(string method, StatusCode status, Exception? unexpected)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DiagnosticsInbox>().RecordServerFailureAsync(method, status, unexpected, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Couldn't record a failed call to {Method}", method);
        }
    }
}
