using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Data;

public static class SermonsImportDataExtensions
{
    public static IServiceCollection AddSermonsImportData(this IServiceCollection services) =>
        services.AddFeatureDbContext<SermonsImportDbContext>(SermonsImportDbContext.SchemaName, migrationOrder: 61);
}
