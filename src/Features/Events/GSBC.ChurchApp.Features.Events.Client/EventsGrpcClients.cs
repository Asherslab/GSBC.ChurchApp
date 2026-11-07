using GSBC.ChurchApp.Features.Events.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Events.Client;

[ProtoGrpc(Model = typeof(EventsContractsModel))]
[ProtoService(typeof(IEventsService))]
[ProtoService(typeof(IEventEditingService))]
[ProtoService(typeof(ICalendarSetupService))]
public sealed partial class EventsGrpcClients : ClientFactory;
