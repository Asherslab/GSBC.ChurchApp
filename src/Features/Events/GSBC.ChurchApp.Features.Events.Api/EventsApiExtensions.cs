using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api;

public static class EventsApiExtensions
{
    public static IServiceCollection AddEventsApi(this IServiceCollection services)
    {
        services.AddEventsData();
        services.AddScoped<EventsPublisher>();
        services.AddScoped<EventWriter>();
        services.AddScoped<CalendarFollowing>();
        services.AddScoped<CalendarLinkSecrets>();
        services.AddScoped<CalendarFiles>();
        services.AddScoped<EventPictures>();
        services.AddScoped<IMediaHolder>(provider => provider.GetRequiredService<EventPictures>());
        services.AddHostedService<EventsSchedule>();
        services.AddScoped<DetailsReminders>();
        services.AddScoped<ICalCopier>();
        services.AddHttpClient<ICalFetcher>(client => client.Timeout = TimeSpan.FromMinutes(1))
            .ConfigurePrimaryHttpMessageHandler(ICalFetcher.PublicInternetOnly);
        services.AddSingleton<ICalSchedule>();
        services.AddHostedService(provider => provider.GetRequiredService<ICalSchedule>());
        services.AddHostedService<SampleCalendar>();

        services.AddPermissions(EventsPermissions.All);
        services.AddNotificationKinds(EventsNotifications.All);
        services.AddDestinations(EventsDestinations.All);
        services.AddDestinationSource<EventDestinationSource>();
        services.AddDestinationSource<CalendarDestinationSource>();

        services.AddSystemRole(new SystemRole(EventsPermissions.EditorRole, "Calendar editor",
            "Adds and changes events and regular events in a calendar, and can make others its editors", ScopeKind.Calendar,
            [EventsPermissions.EditNode], AssignableBy: [EventsPermissions.EditorRole]));
        services.AddSystemRole(new SystemRole(EventsPermissions.PublisherRole, "Calendar publisher",
            "Publishes events in a calendar that needs a publisher", ScopeKind.Calendar,
            [EventsPermissions.EditNode, EventsPermissions.PublishNode], AssignableBy: [EventsPermissions.ManagerRole]));
        services.AddSystemRole(new SystemRole(EventsPermissions.ManagerRole, "Calendars manager",
            "Creates calendars, chooses who each is for, keeps the named breaks, and edits and publishes every calendar", ScopeKind.Global,
            [EventsPermissions.ManageNode, EventsPermissions.EditNode, EventsPermissions.PublishNode]));
        services.AddSystemRole(new SystemRole(EventsPermissions.OrganiserRole, "Event organiser",
            "Reminded when a date of a regular event is missing what it needs", ScopeKind.RegularEvent,
            [EventsPermissions.OrganiseNode], AssignableBy: [EventsPermissions.EditorRole]));
        services.AddSystemRole(new SystemRole(EventsPermissions.AdderRole, "Calendar added",
            "Has added a calendar to their week", ScopeKind.Calendar, [EventsPermissions.HaveNode], AssignableBy: [], PhoneHoldable: true));
        services.AddSystemRole(new SystemRole(EventsPermissions.TurnedOffRole, "Calendar turned off",
            "Has turned off a calendar they'd see anyway", ScopeKind.Calendar, [EventsPermissions.TurnedOffNode], AssignableBy: [], PhoneHoldable: true));

        services.AddFeed(new FeedDefinition(EventsFeeds.Calendars, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(EventsFeeds.PrivateCalendars, FeedAudience.EachRow));
        services.AddFeed(new FeedDefinition(EventsFeeds.Public, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(EventsFeeds.Private, FeedAudience.EachRow));
        services.AddFeed(new FeedDefinition(EventsFeeds.Contacts, FeedAudience.EachRow));
        services.AddFeed(new FeedDefinition(EventsFeeds.Admin, FeedAudience.Holding(EventsPermissions.Edit)));
        return services;
    }

    public static IEndpointRouteBuilder MapEventsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<EventsService>();
        endpoints.MapGrpcService<EventEditingService>();
        endpoints.MapGrpcService<CalendarSetupService>();
        endpoints.MapGrpcService<CalendarFilesService>();
        return endpoints;
    }
}
