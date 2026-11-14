using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Scopes;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Admin;

public static class EventsAdminExtensions
{
    public static IServiceCollection AddEventsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Events", typeof(EventsAdminExtensions).Assembly,
            [new NavEntry("Calendar", CalendarWorkbenchLinks.Page, AppIcons.Calendar, EventsPermissions.Edit, Area: NavAreas.ThisWeek,
                Summary: "The church calendar: events, regular events, breaks and calendars"),
             new NavEntry("Weekly digest", DigestPlaces.Desk, AppIcons.WeeklyDigest, EventsPermissions.Digest, Area: NavAreas.ThisWeek,
                Summary: "What's next at Good Shepherd, made from the calendar, for email and the app")]));
        return services.AddEventsClient().AddSingleton<CalendarEditing>().AddSingleton<IEventEditing, OnlineEventEditing>().AddSingleton<IOnlineAgenda, OnlineAgenda>().AddSingleton<IEventCards, OnlineEventCards>().AddSingleton<IYourCalendarLink, PersonCalendarLink>().AddSingleton<IOfficeWeekSource, EventsOfficeWeek>()
            .AddSingleton<IScopeNamer, CalendarScopeNames>();
    }
}
