using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;

public static class OutreachMapsDataExtensions
{
    public static IServiceCollection AddOutreachMapsData(this IServiceCollection services) =>
        services.AddFeatureDbContext<OutreachMapsDbContext>(OutreachMapsDbContext.SchemaName, migrationOrder: 96);
}
