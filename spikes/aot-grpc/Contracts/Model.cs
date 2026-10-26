using ProtoBuf;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;

namespace Contracts;

[ProtoModel]
[ProtoSerializable(typeof(EventsRequest))]
[ProtoSerializable(typeof(EventsReply))]
[ProtoSerializable(typeof(ChurchEvent))]
public partial class ContractsModel : TypeModel { }

[ProtoGrpc(Model = typeof(ContractsModel))]
[ProtoService(typeof(IEventsService))]
public sealed partial class ContractsClients : ClientFactory { }
