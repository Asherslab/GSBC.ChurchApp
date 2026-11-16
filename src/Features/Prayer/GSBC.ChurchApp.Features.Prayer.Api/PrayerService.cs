using Grpc.Core;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Installs;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class PrayerService(PrayerRequests requests, IAccessEvaluator access) : IPrayerService
{
    public async ValueTask<Done> SendAsync(PrayerRequestSend request, CallContext context = default)
    {
        bool mayPostAsTheChurch = request.FromChurch && context.ServerCallContext?.GetHttpContext().User is { } user
            && await access.IsAllowedAsync(user, PrayerPermissions.WallNode, null, context.CancellationToken);
        await requests.SendAsync(CallingInstall.Of(context), CallerId(context), request, mayPostAsTheChurch, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> PrayAsync(PrayMark request, CallContext context = default)
    {
        await requests.PrayAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> AnsweredAsync(AnsweredSave request, CallContext context = default)
    {
        await requests.AnsweredAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> TakeDownAsync(RequestRef request, CallContext context = default)
    {
        await requests.TakeDownAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteAsync(RequestRef request, CallContext context = default)
    {
        await requests.DeleteAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);
        return new Done();
    }

    private static Guid? CallerId(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User is { } user ? Caller.UserId(user) : null;
}
