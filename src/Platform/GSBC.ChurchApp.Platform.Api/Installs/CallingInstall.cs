using Grpc.Core;
using Microsoft.AspNetCore.Http;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Platform.Api.Installs;

public static class CallingInstall
{
    private static readonly object InstallKey = new();

    public static Guid Of(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().Items[InstallKey] as Guid?
        ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "This call needs a proven app install."));

    public static Guid? Find(CallContext context) => context.ServerCallContext?.GetHttpContext().Items[InstallKey] as Guid?;

    internal static void Set(HttpContext context, Guid installId) => context.Items[InstallKey] = installId;
}
