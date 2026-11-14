using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class EventsNotifications
{
    public static readonly NotificationKind Told = new(
        "events.told", "News about an event in a calendar you have", "Calendar", PerScope: ScopeKind.Calendar, MayBeUrgent: true, Opens: new("/event/{id}"));

    public static readonly NotificationKind AskedToPublish = new(
        "events.publish.asked", "An event is waiting for you to publish it", "Calendar", PerScope: ScopeKind.Calendar,
        For: [EventsPermissions.Publish], InAdminPanel: true, Opens: new("/calendar/{id}"));

    public static readonly NotificationKind AskedToShow = new(
        "events.showing.asked", "Another calendar asks to show one of its events on yours", "Calendar", PerScope: ScopeKind.Calendar,
        For: [EventsPermissions.Edit], InAdminPanel: true, Opens: new("/calendar/{id}"));

    public static readonly NotificationKind AskedToShowElsewhere = new(
        "events.showing.requested", "Another calendar asks to show one of your events on it", "Calendar", PerScope: ScopeKind.Calendar,
        For: [EventsPermissions.Edit], InAdminPanel: true, Opens: new("/calendar/{id}"));

    public static readonly NotificationKind DetailsNeeded = new(
        "events.details.needed", "A date of a regular event you organise is missing what it needs", "Calendar", PerScope: ScopeKind.Calendar,
        Waits: true, For: [EventsPermissions.Organise, EventsPermissions.Edit], Opens: new("/event/{id}"));

    public static readonly NotificationKind DigestOut = new(
        "events.digest.out", "What's next, the weekly digest", "Calendar", Opens: new("/whats-next"));

    public static readonly IReadOnlyList<NotificationKind> All = [Told, AskedToPublish, AskedToShow, AskedToShowElsewhere, DetailsNeeded, DigestOut];
}
