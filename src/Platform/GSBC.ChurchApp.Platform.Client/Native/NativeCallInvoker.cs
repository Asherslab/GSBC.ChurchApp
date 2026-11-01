using Grpc.Core;

namespace GSBC.ChurchApp.Platform.Client.Native;

public sealed class NativeCallInvoker(NativeBridge bridge) : CallInvoker
{
    private static readonly RpcException StreamingNotBuilt =
        new(new Status(StatusCode.Unimplemented, "Streaming through the native layer arrives with the data layer milestone."));

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
        new(SendAsync(method, request, options.CancellationToken),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { });

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
        throw new RpcException(new Status(StatusCode.Unimplemented, "WebAssembly can't block; use the async call."));

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
        throw StreamingNotBuilt;

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options) =>
        throw StreamingNotBuilt;

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options) =>
        throw StreamingNotBuilt;

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        Method<TRequest, TResponse> method, TRequest request, CancellationToken cancellationToken)
    {
        BufferSerializationContext serialized = new();
        method.RequestMarshaller.ContextualSerializer(request, serialized);
        NativeUnaryResult result = await bridge.UnaryAsync(method.FullName, serialized.Payload, cancellationToken);

        if (result.Status != (int)StatusCode.OK)
            throw new RpcException(new Status((StatusCode)result.Status, result.Message ?? ""));

        return method.ResponseMarshaller.ContextualDeserializer(
            new BufferDeserializationContext(Convert.FromBase64String(result.Response ?? "")));
    }
}
