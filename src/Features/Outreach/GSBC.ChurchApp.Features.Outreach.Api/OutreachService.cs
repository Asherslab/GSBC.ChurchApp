using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Installs;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachService(Walking walking) : IOutreachService
{
    public async ValueTask<Done> RecordWalkAsync(WalkPiece request, CallContext context = default)
    {
        await walking.RecordAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> NoJunkMailAsync(NoJunkMail request, CallContext context = default)
    {
        await walking.NoJunkMailAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> UndoNoJunkMailAsync(MarkRef request, CallContext context = default)
    {
        await walking.UndoNoJunkMailAsync(CallingInstall.Of(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DoorAsync(DoorOutcome request, CallContext context = default)
    {
        await walking.DoorAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> ReportAsync(ReportSend request, CallContext context = default)
    {
        await walking.ReportAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> ClaimAsync(ClaimSend request, CallContext context = default)
    {
        await walking.ClaimAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> UnclaimAsync(MarkRef request, CallContext context = default)
    {
        await walking.UnclaimAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> CountingAsync(CountingSave request, CallContext context = default)
    {
        Guid user = CallerId(context) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));
        await walking.CountingAsync(user, request, context.CancellationToken);
        return new Done();
    }

    private static Guid? CallerId(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User is { } user ? Caller.UserId(user) : null;
}
