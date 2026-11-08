using GSBC.ChurchApp.Features.Events.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Events.Client;

[ProtoGrpc(Model = typeof(EventsContractsModel))]
[ProtoService(typeof(IEventsService))]
[ProtoService(typeof(IEventEditingService))]
[ProtoService(typeof(ICalendarSetupService))]
[ProtoService(typeof(ICalendarFilesService))]
[ProtoService(typeof(IDigestService))]
public sealed partial class EventsGrpcClients : ClientFactory;
