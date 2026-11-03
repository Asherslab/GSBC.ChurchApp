using Grpc.AspNetCore.Server;
using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Analytics.Api;

public static class AnalyticsApiExtensions
{
    public static IServiceCollection AddAnalyticsApi(this IServiceCollection services)
    {
        services.AddAnalyticsData();
        services.AddSingleton<AnalyticsEnvironment>();
        services.AddScoped<NewErrorKinds>();
        services.AddScoped<ReportsSeen>();
        services.AddScoped<DiagnosticsInbox>();
        services.AddScoped<UsageInbox>();
        services.AddScoped<AnalyticsRetention>();
        services.AddHostedService<AnalyticsRetentionService>();
        services.AddSingleton<ServerErrorInterceptor>();
        services.Configure<GrpcServiceOptions>(ObserveBeforeTiers);
        services.AddPermissions(AnalyticsPermissions.All);
        services.AddMeasures(AnalyticsMeasures.All);
        services.AddSystemRole(new SystemRole(AnalyticsPermissions.InsightsViewerRole, "Insights viewer",
            "See how the app is used: how many phones open it, what's popular, whether notices were seen. Totals only, never anything about a person",
            ScopeKind.Global, [AnalyticsPermissions.ViewNode]));
        services.AddSystemRole(new SystemRole(AnalyticsPermissions.DeveloperRole, "Developer",
            "See the app's errors and failed calls, with their technical detail, and get an email when a new kind of error appears. For whoever builds the app",
            ScopeKind.Global, [AnalyticsPermissions.HealthViewNode]));
        return services;
    }

    private static void ObserveBeforeTiers(GrpcServiceOptions options)
    {
        options.Interceptors.Add<ServerErrorInterceptor>();
        InterceptorRegistration observer = options.Interceptors[^1];
        options.Interceptors.RemoveAt(options.Interceptors.Count - 1);
        options.Interceptors.Insert(0, observer);
    }

    public static IEndpointRouteBuilder MapAnalyticsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<DiagnosticsService>();
        endpoints.MapGrpcService<HealthService>();
        endpoints.MapGrpcService<AnalyticsService>();
        endpoints.MapGrpcService<InsightsService>();
        return endpoints;
    }
}
