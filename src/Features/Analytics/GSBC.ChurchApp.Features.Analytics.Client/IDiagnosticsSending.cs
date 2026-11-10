using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Client.Outbox;

namespace GSBC.ChurchApp.Features.Analytics.Client;

public interface IDiagnosticsSending
{
    Task SendAsync(DiagnosticsReport report);
}

public sealed class QueuedDiagnostics(IOutbox outbox) : IDiagnosticsSending
{
    public async Task SendAsync(DiagnosticsReport report)
    {
        if (!outbox.IsAvailable)
            return;
        using MemoryStream body = new();
        AnalyticsContractsModel.Instance.Serialize(body, report);
        await outbox.EnqueueQuietlyAsync(IDiagnosticsService.ReportMethod, body.ToArray());
    }
}
