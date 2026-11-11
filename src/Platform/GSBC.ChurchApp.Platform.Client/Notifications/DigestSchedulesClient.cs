using Grpc.Core;
using GSBC.ChurchApp.Platform.Client.Online;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Platform.Client.Notifications;

[AdminOnly]
public sealed class DigestSchedulesClient(ChannelBase channel)
{
    private readonly IDigestSchedulesService _schedules = channel.CreateGrpcService<IDigestSchedulesService>();

    public async Task<DigestScheduleMessage> GetAsync(string kind) => await _schedules.GetAsync(new DigestScheduleRequest { Kind = kind });

    public async Task<DigestScheduleMessage> ChangeAsync(DigestScheduleChange change) => await _schedules.ChangeAsync(change);
}
