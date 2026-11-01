using GSBC.ChurchApp.Features.Accounts.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Accounts.Client;

[ProtoGrpc(Model = typeof(AccountsContractsModel))]
[ProtoService(typeof(ISessionService))]
[ProtoService(typeof(IAccountsService))]
public sealed partial class AccountsGrpcClients : ClientFactory;
