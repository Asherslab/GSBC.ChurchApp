using Grpc.Core;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Analytics.Client;

[AdminOnly]
public sealed class InsightsClient(ChannelBase channel)
{
    private readonly IInsightsService _insights = channel.CreateGrpcService<IInsightsService>();

    public ValueTask<InsightsSummary> GetSummaryAsync() => _insights.GetSummaryAsync(new InsightsQuery());
}
