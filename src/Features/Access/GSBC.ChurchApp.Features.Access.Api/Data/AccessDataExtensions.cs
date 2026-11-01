using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Access.Api.Data;

public static class AccessDataExtensions
{
    public static IServiceCollection AddAccessData(this IServiceCollection services) =>
        services.AddFeatureDbContext<AccessDbContext>(AccessDbContext.SchemaName, migrationOrder: 20);
}
