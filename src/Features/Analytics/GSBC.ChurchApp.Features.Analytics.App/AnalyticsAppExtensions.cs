using GSBC.ChurchApp.Features.Analytics.Client;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Analytics.App;

public static class AnalyticsAppExtensions
{
    public static IServiceCollection AddAnalyticsApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Analytics", typeof(AnalyticsAppExtensions).Assembly,
            [new NavEntry("Help improve the app", "help-improve", AppIcons.HelpImproveTheApp)]));
        return services.AddAnalyticsClient(ReportOrigin.App).AddUsageCounting().AddSingleton<IDiagnosticsSending, QueuedDiagnostics>();
    }
}
