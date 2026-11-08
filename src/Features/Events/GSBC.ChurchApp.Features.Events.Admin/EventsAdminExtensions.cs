using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Scopes;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Admin;

public static class EventsAdminExtensions
{
    public static IServiceCollection AddEventsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Events", typeof(EventsAdminExtensions).Assembly,
            [new NavEntry("Calendar", CalendarWorkbenchLinks.Page, "calendar", EventsPermissions.Edit, Group: NavGroups.Content,
                Summary: "The church calendar: events, regular events, breaks and calendars"),
             new NavEntry("Weekly digest", DigestPlaces.Desk, "megaphone", EventsPermissions.Digest, Group: NavGroups.Content,
                Summary: "What's next at Good Shepherd, made from the calendar, for email and the app")]));
        return services.AddEventsClient().AddSingleton<IYourCalendarLink, PersonCalendarLink>().AddSingleton<IOfficeWeekSource, EventsOfficeWeek>()
            .AddSingleton<IScopeNamer, CalendarScopeNames>();
    }
}
