using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Events.Contracts;

[Service("gsbc.events.Editing")]
public interface IEventEditingService
{
    [SignedIn]
    ValueTask<CalendarDesk> DeskAsync(Done request, CallContext context = default);

    [SignedIn]
    ValueTask<Agenda> AgendaAsync(AgendaQuery request, CallContext context = default);

    [Requires(EventsPermissions.EditNode)]
    ValueTask<EventDetail> EventAsync(EventRef request, CallContext context = default);

    [Requires(EventsPermissions.EditNode)]
    ValueTask<EventDetail> SaveEventAsync(EventSave request, CallContext context = default);

    [Requires(EventsPermissions.EditNode)]
    ValueTask<Done> DeleteEventAsync(EventRef request, CallContext context = default);

    [Requires(EventsPermissions.EditNode)]
    ValueTask<RegularEventDetail> RegularEventAsync(RegularRef request, CallContext context = default);

    [Requires(EventsPermissions.EditNode)]
    ValueTask<RegularEventSaved> SaveRegularEventAsync(RegularEventSave request, CallContext context = default);

    [Requires(EventsPermissions.EditNode)]
    ValueTask<Done> DeleteRegularEventAsync(RegularRef request, CallContext context = default);

    [Requires(EventsPermissions.EditNode)]
    ValueTask<RegularEventDetail> SaveDateAsync(DateSave request, CallContext context = default);

    [Requires(EventsPermissions.EditNode)]
    ValueTask<Done> AskToPublishAsync(ItemRef request, CallContext context = default);

    [Requires(EventsPermissions.PublishNode)]
    ValueTask<Done> PublishAsync(ItemRef request, CallContext context = default);

    [SignedIn]
    ValueTask<AskedList> AskedToPublishAsync(Done request, CallContext context = default);

    [Requires(EventsPermissions.EditNode)]
    ValueTask<Done> ShowOnAsync(ShowOnRequest request, CallContext context = default);

    [Requires(EventsPermissions.EditNode)]
    ValueTask<Done> AnswerShowingAsync(ShowingAnswer request, CallContext context = default);

    [Requires(EventsPermissions.EditNode)]
    ValueTask<Done> TellPeopleAsync(TellPeople request, CallContext context = default);

    [SignedIn]
    ValueTask<NeedsDetailsList> NeedsDetailsAsync(Done request, CallContext context = default);

    [SignedIn]
    ValueTask<PlacesUsed> PlacesUsedAsync(Done request, CallContext context = default);

    [SignedIn]
    ValueTask<BreakList> BreaksAsync(Done request, CallContext context = default);
}
