using Grpc.Core;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Analytics.Client;

[AdminOnly]
public sealed class OnlineDiagnostics(ChannelBase channel) : IDiagnosticsSending
{
    private readonly IDiagnosticsService _diagnostics = channel.CreateGrpcService<IDiagnosticsService>();

    public async Task SendAsync(DiagnosticsReport report) => await _diagnostics.ReportAsync(report);
}
