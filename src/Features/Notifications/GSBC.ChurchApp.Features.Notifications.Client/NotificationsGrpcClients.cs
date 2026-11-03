using GSBC.ChurchApp.Features.Notifications.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Notifications.Client;

[ProtoGrpc(Model = typeof(NotificationsContractsModel))]
[ProtoService(typeof(INotificationsService))]
[ProtoService(typeof(IAnnouncementsService))]
public sealed partial class NotificationsGrpcClients : ClientFactory;
