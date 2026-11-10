using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.People;
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
        services.AddScoped<IPersonData, EventsPersonData>();
        services.AddScoped<EventWriter>();
        services.AddScoped<CalendarFollowing>();
        services.AddScoped<CalendarRemover>();
        services.AddScoped<CalendarLinkSecrets>();
        services.AddScoped<CalendarFiles>();
        services.AddScoped<EventPictures>();
        services.AddScoped<IMediaHolder>(provider => provider.GetRequiredService<EventPictures>());
        services.AddScoped<IScopeObjects, CalendarScopeObjects>();
        services.AddScoped<IMinistryCalendars, MinistryCalendars>();
        services.AddScoped<IScopeAttachments, MinistryCalendarAttachments>();
        services.AddScoped<IScopeObjects, RegularEventScopeObjects>();
        services.AddHostedService<EventsSchedule>();
        services.AddScoped<DetailsReminders>();
        services.AddScoped<ICalCopier>();
        services.AddHttpClient<ICalFetcher>(client => client.Timeout = TimeSpan.FromMinutes(1))
            .ConfigurePrimaryHttpMessageHandler(ICalFetcher.PublicInternetOnly);
        services.AddSingleton<ICalSchedule>();
        services.AddHostedService(provider => provider.GetRequiredService<ICalSchedule>());
        services.AddHostedService<SampleCalendar>();

        services.AddScoped<IRoleUses, EventsRoleUses>();
        services.AddPermissions(EventsPermissions.All);
        services.AddNotificationKinds(EventsNotifications.All);
        services.AddDestinations(EventsDestinations.All);
        services.AddDestinationSource<EventDestinationSource>();
        services.AddDestinationSource<CalendarDestinationSource>();

        services.AddSystemRole(new SystemRole(EventsPermissions.EveryCalendarEditorRole, "Calendar Editor",
            "Add and change events and regular events in every calendar", ScopeKind.Global,
            [EventsPermissions.EditNode], Feature: RoleFeatures.Calendar, Kits: [RoleKits.Pictures]));
        services.AddSystemRole(new SystemRole(EventsPermissions.EveryCalendarPublisherRole, "Calendar Publisher",
            "Add, change and publish events in every calendar", ScopeKind.Global,
            [EventsPermissions.EditNode, EventsPermissions.PublishNode], Feature: RoleFeatures.Calendar, Kits: [RoleKits.Pictures]));
        services.AddSystemRole(new SystemRole(EventsPermissions.ManagerRole, "Calendar Manager",
            "Make and remove calendars, choose who each is for, keep the named breaks, and edit and publish every calendar", ScopeKind.Global,
            [EventsPermissions.ManageNode, EventsPermissions.EditNode, EventsPermissions.PublishNode], Feature: RoleFeatures.Calendar, Kits: [RoleKits.Pictures]));
        services.AddSystemRole(new SystemRole(EventsPermissions.DigestMakerRole, "Digest Maker",
            "Make the weekly digest from the calendar, copy it for email and send it to the app", ScopeKind.Global,
            [EventsPermissions.DigestNode], Feature: RoleFeatures.Calendar));
        services.AddSystemRole(new SystemRole(EventsPermissions.EditorRole, "Editor",
            "Add and change this calendar's events and regular events, and add other editors", ScopeKind.Calendar,
            [EventsPermissions.EditNode], AssignableBy: [EventsPermissions.EditorRole, EventsPermissions.EveryCalendarEditorRole, EventsPermissions.EveryCalendarPublisherRole, EventsPermissions.ManagerRole], Kits: [RoleKits.Pictures]));
        services.AddSystemRole(new SystemRole(EventsPermissions.PublisherRole, "Publisher",
            "Add, change and publish this calendar's events", ScopeKind.Calendar,
            [EventsPermissions.EditNode, EventsPermissions.PublishNode], AssignableBy: [EventsPermissions.EveryCalendarPublisherRole, EventsPermissions.ManagerRole], Kits: [RoleKits.Pictures]));
        services.AddSystemRole(new SystemRole(EventsPermissions.OrganiserRole, "Organiser",
            "Reminded when a date of this regular event is missing what it needs", ScopeKind.RegularEvent,
            [EventsPermissions.OrganiseNode],
            AssignableBy: [EventsPermissions.EveryCalendarEditorRole, EventsPermissions.EveryCalendarPublisherRole, EventsPermissions.ManagerRole]));
        services.AddSystemRole(new SystemRole(EventsPermissions.AdderRole, "Calendar added",
            "Has added a calendar to their week", ScopeKind.Calendar, [EventsPermissions.HaveNode], AssignableBy: [], PhoneHoldable: true, Hidden: true));
        services.AddSystemRole(new SystemRole(EventsPermissions.TurnedOffRole, "Calendar turned off",
            "Has turned off a calendar they'd see anyway", ScopeKind.Calendar, [EventsPermissions.TurnedOffNode], AssignableBy: [], PhoneHoldable: true, Hidden: true));

        services.AddSurfaces(EventsTiles.Surfaces);
        services.AddFeed(new FeedDefinition(EventsFeeds.Calendars, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(EventsFeeds.PrivateCalendars, FeedAudience.EachRow));
        services.AddFeed(new FeedDefinition(EventsFeeds.Public, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(EventsFeeds.Private, FeedAudience.EachRow));
        services.AddFeed(new FeedDefinition(EventsFeeds.Contacts, FeedAudience.EachRow));
        services.AddFeed(new FeedDefinition(EventsFeeds.Admin, FeedAudience.Holding(EventsPermissions.Edit)));
        services.AddFeed(new FeedDefinition(EventsFeeds.Digest, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(EventsFeeds.PrivateDigest, FeedAudience.EachRow));
        return services;
    }

    public static IEndpointRouteBuilder MapEventsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<EventsService>();
        endpoints.MapGrpcService<EventEditingService>();
        endpoints.MapGrpcService<CalendarSetupService>();
        endpoints.MapGrpcService<CalendarFilesService>();
        endpoints.MapGrpcService<DigestService>();
        return endpoints;
    }
}
