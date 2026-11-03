using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Client.Analytics;
using GSBC.ChurchApp.Platform.Client.Diagnostics;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Analytics.Client;

public static class AnalyticsClientExtensions
{
    public static IServiceCollection AddAnalyticsClient(this IServiceCollection services, ReportOrigin origin)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddDestinations(AnalyticsDestinations.All);
        services.AddSingleton<AnalyticsConsent>();
        services.AddSingleton(provider => new ErrorReporter(provider, provider.GetRequiredService<AnalyticsConsent>(),
            provider.GetRequiredService<CurrentPage>(), provider.GetRequiredService<TimeProvider>(), origin));
        services.AddSingleton<IClientDiagnostics>(provider => provider.GetRequiredService<ErrorReporter>());
        services.AddSingleton<IClientStartupTask>(provider => provider.GetRequiredService<ErrorReporter>());
        services.AddSingleton<ILoggerProvider, ErrorLoggerProvider>();
        services.AddSingleton<HealthClient>();
        services.AddSingleton<InsightsClient>();
        return services;
    }

    public static IServiceCollection AddUsageCounting(this IServiceCollection services)
    {
        services.AddSingleton<UsageCounter>();
        services.AddSingleton<IUsage>(provider => provider.GetRequiredService<UsageCounter>());
        services.AddSingleton<IClientStartupTask>(provider => provider.GetRequiredService<UsageCounter>());
        return services;
    }
}
