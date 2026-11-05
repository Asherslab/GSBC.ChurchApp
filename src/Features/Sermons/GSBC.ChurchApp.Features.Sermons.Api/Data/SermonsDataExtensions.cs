using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Data;

public static class SermonsDataExtensions
{
    public static IServiceCollection AddSermonsData(this IServiceCollection services) =>
        services.AddFeatureDbContext<SermonsDbContext>(SermonsDbContext.SchemaName, migrationOrder: 60);
}
