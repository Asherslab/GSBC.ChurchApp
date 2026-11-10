using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Ministries.Api.Data;

public static class MinistriesDataExtensions
{
    public static IServiceCollection AddMinistriesData(this IServiceCollection services) =>
        services.AddFeatureDbContext<MinistriesDbContext>(MinistriesDbContext.SchemaName, migrationOrder: 75);
}
