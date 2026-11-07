using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Admin;

public static class EventsAdminExtensions
{
    public static IServiceCollection AddEventsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Events", typeof(EventsAdminExtensions).Assembly, []));
        return services.AddEventsClient();
    }
}
