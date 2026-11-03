using Grpc.Core;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Analytics.Client;

public sealed class InsightsClient(ChannelBase channel)
{
    private readonly IInsightsService _insights = channel.CreateGrpcService<IInsightsService>();

    public ValueTask<InsightsSummary> GetSummaryAsync() => _insights.GetSummaryAsync(new InsightsQuery());
}
