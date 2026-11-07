using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Installs;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventsService(CalendarFollowing following, CalendarLinkSecrets links) : IEventsService
{
    public async ValueTask<MyCalendars> ChooseAsync(CalendarChoice request, CallContext context = default) =>
        await following.ChooseAsync(CallingInstall.Of(context), context.ServerCallContext?.GetHttpContext().User ?? new System.Security.Claims.ClaimsPrincipal(), request, context.CancellationToken);

    public async ValueTask<MyCalendars> MineAsync(Done request, CallContext context = default)
    {
        Guid installId = CallingInstall.Of(context);
        if (CallerId(context) is { } person)
            await links.ClaimForPersonAsync(installId, person, context.CancellationToken);
        return await following.MineAsync(installId, CallerId(context), context.CancellationToken);
    }

    private static Guid? CallerId(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User is { } user ? Caller.UserId(user) : null;
}
