using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Api.Data;

public static class OutreachDataExtensions
{
    public static IServiceCollection AddOutreachData(this IServiceCollection services) =>
        services.AddFeatureDbContext<OutreachDbContext>(OutreachDbContext.SchemaName, migrationOrder: 90);
}
