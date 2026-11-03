using GSBC.ChurchApp.Features.Analytics.Client;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Analytics.Admin;

public static class AnalyticsAdminExtensions
{
    public static IServiceCollection AddAnalyticsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Analytics", typeof(AnalyticsAdminExtensions).Assembly,
            [new NavEntry("Insights", "insights", "sparkles", AnalyticsPermissions.View, Group: NavGroups.People, Summary: "How many phones open the app, and what's popular"),
             new NavEntry("App health", "health", "shield-check", AnalyticsPermissions.HealthView, Group: NavGroups.You, Summary: "Errors from phones, the admin panel and the server")]));
        return services.AddAnalyticsClient(ReportOrigin.Admin);
    }
}
