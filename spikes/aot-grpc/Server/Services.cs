using Contracts;
using ProtoBuf.Grpc.Configuration;

[ProtoGrpc(Model = typeof(ContractsModel))]
[ProtoService(typeof(IEventsService), typeof(EventsService))]
public sealed partial class ServerServices : ClientFactory { }
