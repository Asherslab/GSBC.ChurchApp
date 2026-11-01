using Grpc.Core;

namespace GSBC.ChurchApp.Platform.Client.Native;

public sealed class NativeChannel(NativeBridge bridge) : ChannelBase("native")
{
    public override CallInvoker CreateCallInvoker() => new NativeCallInvoker(bridge);
}
