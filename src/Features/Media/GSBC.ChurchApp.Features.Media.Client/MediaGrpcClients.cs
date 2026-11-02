using GSBC.ChurchApp.Features.Media.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Media.Client;

[ProtoGrpc(Model = typeof(MediaContractsModel))]
[ProtoService(typeof(IMediaService))]
[ProtoService(typeof(IFontsService))]
public sealed partial class MediaGrpcClients : ClientFactory;
