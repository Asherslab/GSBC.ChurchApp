using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Media.Api.Data;

public static class MediaDataExtensions
{
    public static IServiceCollection AddMediaData(this IServiceCollection services) =>
        services.AddFeatureDbContext<MediaDbContext>(MediaDbContext.SchemaName, migrationOrder: 30);
}
