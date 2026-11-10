using GSBC.ChurchApp.Features.Events.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Events.Admin;

[ProtoGrpc(Model = typeof(EventsContractsModel))]
[ProtoService(typeof(IEventEditingService))]
[ProtoService(typeof(ICalendarSetupService))]
[ProtoService(typeof(IDigestService))]
public sealed partial class EventsAdminGrpcClients : ClientFactory;
