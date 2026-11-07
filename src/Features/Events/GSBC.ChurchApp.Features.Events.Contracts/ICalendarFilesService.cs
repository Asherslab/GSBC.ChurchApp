using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Events.Contracts;

[Service("gsbc.events.CalendarFiles")]
public interface ICalendarFilesService
{
    [Public]
    ValueTask<CalendarFile> ReadAsync(CalendarFileRequest request, CallContext context = default);

    [AppInstall]
    ValueTask<CalendarLink> LinkAsync(Done request, CallContext context = default);

    [AppInstall]
    ValueTask<CalendarLink> ResetLinkAsync(Done request, CallContext context = default);

    [SignedIn]
    ValueTask<CalendarLink> PersonLinkAsync(Done request, CallContext context = default);

    [SignedIn]
    ValueTask<CalendarLink> ResetPersonLinkAsync(Done request, CallContext context = default);
}
