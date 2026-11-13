using GSBC.ChurchApp.Features.News.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.News.Client;

[ProtoGrpc(Model = typeof(NewsContractsModel))]
[ProtoService(typeof(INewsService))]
public sealed partial class NewsGrpcClients : ClientFactory;
