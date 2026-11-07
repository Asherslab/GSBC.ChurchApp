using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Data;

public static class EventsDataExtensions
{
    public static IServiceCollection AddEventsData(this IServiceCollection services) =>
        services.AddFeatureDbContext<EventsDbContext>(EventsDbContext.SchemaName, migrationOrder: 70);
}
