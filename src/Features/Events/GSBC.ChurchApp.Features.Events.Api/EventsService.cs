using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Installs;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventsService(CalendarFollowing following) : IEventsService
{
    public async ValueTask<MyCalendars> ChooseAsync(CalendarChoice request, CallContext context = default) =>
        await following.ChooseAsync(CallingInstall.Of(context), CallerId(context), request, context.CancellationToken);

    public async ValueTask<MyCalendars> MineAsync(Done request, CallContext context = default) =>
        await following.MineAsync(CallingInstall.Of(context), CallerId(context), context.CancellationToken);

    private static Guid? CallerId(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User is { } user ? Caller.UserId(user) : null;
}
