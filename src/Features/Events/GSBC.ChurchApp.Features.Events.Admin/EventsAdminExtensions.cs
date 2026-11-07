using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Admin;

public static class EventsAdminExtensions
{
    public static IServiceCollection AddEventsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Events", typeof(EventsAdminExtensions).Assembly,
            [new NavEntry("Calendar", CalendarWorkbenchLinks.Page, "calendar", EventsPermissions.Edit, Group: NavGroups.Content,
                Summary: "The church calendar: events, regular events, breaks and calendars")]));
        return services.AddEventsClient().AddSingleton<IOfficeWeekSource, EventsOfficeWeek>();
    }
}
