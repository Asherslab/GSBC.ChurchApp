using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Configuration.Api.Data;

public static class ConfigurationDataExtensions
{
    public static IServiceCollection AddConfigurationData(this IServiceCollection services) =>
        services.AddFeatureDbContext<ConfigurationDbContext>(ConfigurationDbContext.SchemaName, migrationOrder: 30);
}
