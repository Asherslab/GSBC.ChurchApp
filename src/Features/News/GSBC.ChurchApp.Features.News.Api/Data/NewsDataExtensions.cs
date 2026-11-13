using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.News.Api.Data;

public static class NewsDataExtensions
{
    public static IServiceCollection AddNewsData(this IServiceCollection services) =>
        services.AddFeatureDbContext<NewsDbContext>(NewsDbContext.SchemaName, migrationOrder: 86);
}
