using GSBC.ChurchApp.Features.Chats.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Chats.Client;

[ProtoGrpc(Model = typeof(ChatsContractsModel))]
[ProtoService(typeof(IChatsService))]
[ProtoService(typeof(IChatOversightService))]
public sealed partial class ChatsGrpcClients : ClientFactory;
