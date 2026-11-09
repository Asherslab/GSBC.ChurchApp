using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.People.Api.Data;

public static class PeopleDataExtensions
{
    public static IServiceCollection AddPeopleData(this IServiceCollection services) =>
        services.AddFeatureDbContext<PeopleDbContext>(PeopleDbContext.SchemaName, migrationOrder: 15);
}
