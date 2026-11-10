using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.Api.Data;

public static class PrayerDataExtensions
{
    public static IServiceCollection AddPrayerData(this IServiceCollection services) =>
        services.AddFeatureDbContext<PrayerDbContext>(PrayerDbContext.SchemaName, migrationOrder: 80);
}
