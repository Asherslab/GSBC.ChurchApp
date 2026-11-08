using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Events.Contracts;

[Service("gsbc.events.Setup")]
public interface ICalendarSetupService
{
    [Requires(EventsPermissions.ManageNode)]
    ValueTask<CalendarSettings> SaveCalendarAsync(CalendarSettings request, CallContext context = default);

    [Requires(EventsPermissions.ManageNode)]
    ValueTask<CalendarRemoval> RemovalAsync(CalendarToRemove request, CallContext context = default);

    [Requires(EventsPermissions.ManageNode)]
    ValueTask<Done> RemoveCalendarAsync(CalendarToRemove request, CallContext context = default);

    [Requires(EventsPermissions.ManageNode)]
    ValueTask<BreakInfo> SaveBreakAsync(BreakInfo request, CallContext context = default);

    [Requires(EventsPermissions.ManageNode)]
    ValueTask<Done> DeleteBreakAsync(BreakRef request, CallContext context = default);
}
