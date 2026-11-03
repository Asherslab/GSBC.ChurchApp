using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Analytics.Api.Data;

public static class AnalyticsDataExtensions
{
    public static IServiceCollection AddAnalyticsData(this IServiceCollection services) =>
        services.AddFeatureDbContext<AnalyticsDbContext>(AnalyticsDbContext.SchemaName, migrationOrder: 50);
}
