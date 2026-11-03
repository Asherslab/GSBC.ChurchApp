using Grpc.Core;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Analytics.Client;

public sealed class HealthClient(ChannelBase channel)
{
    private readonly IHealthService _health = channel.CreateGrpcService<IHealthService>();

    public ValueTask<HealthOverview> GetOverviewAsync(int days) => _health.GetOverviewAsync(new HealthQuery { Days = days });

    public ValueTask<ErrorKindDetail> GetErrorKindAsync(Guid id) => _health.GetErrorKindAsync(new ErrorKindRequest { Id = id });
}
