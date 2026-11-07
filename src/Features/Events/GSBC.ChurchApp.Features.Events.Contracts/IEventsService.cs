using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Events.Contracts;

[Service("gsbc.events.Events")]
public interface IEventsService
{
    [AppInstall]
    ValueTask<MyCalendars> ChooseAsync(CalendarChoice request, CallContext context = default);

    [AppInstall]
    ValueTask<MyCalendars> MineAsync(Done request, CallContext context = default);
}
