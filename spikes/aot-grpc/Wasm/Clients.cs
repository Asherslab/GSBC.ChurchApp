using Contracts;
using ProtoBuf.Grpc.Configuration;

[ProtoGrpc(Model = typeof(ContractsModel))]
[ProtoService(typeof(IEventsService))]
public sealed partial class AppClients : ClientFactory { }
