using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Rosters.Api.Data;

public static class RostersDataExtensions
{
    public static IServiceCollection AddRostersData(this IServiceCollection services) =>
        services.AddFeatureDbContext<RostersDbContext>(RostersDbContext.SchemaName, migrationOrder: 80);
}
