using Grpc.Core;
using Grpc.Core.Interceptors;

namespace GSBC.ChurchApp.Platform.Client.Diagnostics;

public sealed class ObservedChannel(ChannelBase inner, IServiceProvider services) : ChannelBase(inner.Target)
{
    public override CallInvoker CreateCallInvoker() => inner.CreateCallInvoker().Intercept(new FailedCallObserver(services));

    private sealed class FailedCallObserver(IServiceProvider services) : Interceptor
    {
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            AsyncUnaryCall<TResponse> call = continuation(request, context);
            return new AsyncUnaryCall<TResponse>(ObserveAsync(call.ResponseAsync, context.Method.FullName),
                call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
        }

        private async Task<TResponse> ObserveAsync<TResponse>(Task<TResponse> response, string method)
        {
            try
            {
                return await response;
            }
            catch (RpcException failure)
            {
                (services.GetService(typeof(IClientDiagnostics)) as IClientDiagnostics)?.CallFailed(method, failure.StatusCode);
                throw;
            }
        }
    }
}
