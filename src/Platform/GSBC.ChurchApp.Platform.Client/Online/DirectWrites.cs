using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using GSBC.ChurchApp.Platform.Client.Outbox;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Platform.Client.Online;

[AdminOnly]
public sealed class DirectWrites(ChannelBase channel) : IWrites
{
    private static readonly Marshaller<byte[]> Bytes = Marshallers.Create(bytes => bytes, bytes => bytes);

    public bool Queued => false;

    public async ValueTask SendAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string method, T request, TypeModel model)
    {
        using MemoryStream body = new();
        model.Serialize(body, request);
        string[] parts = method.TrimStart('/').Split('/', 2);
        Method<byte[], byte[]> call = new(MethodType.Unary, parts[0], parts[1], Bytes, Bytes);
        await channel.CreateCallInvoker().AsyncUnaryCall(call, null, new CallOptions(), body.ToArray());
    }
}
