using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Notifications.Api.Data;

public static class NotificationsDataExtensions
{
    public static IServiceCollection AddNotificationsData(this IServiceCollection services) =>
        services.AddFeatureDbContext<NotificationsDbContext>(NotificationsDbContext.SchemaName, migrationOrder: 55);
}
