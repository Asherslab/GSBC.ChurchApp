using GSBC.ChurchApp.Features.Events.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Events.Client;

[ProtoGrpc(Model = typeof(EventsContractsModel))]
[ProtoService(typeof(IEventsService))]
[ProtoService(typeof(ICalendarFilesService))]
public sealed partial class EventsGrpcClients : ClientFactory;
