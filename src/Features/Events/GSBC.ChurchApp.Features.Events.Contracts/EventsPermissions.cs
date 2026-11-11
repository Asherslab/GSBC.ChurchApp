using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class EventsPermissions
{
    public const string EditNode = "events.edit";
    public const string PublishNode = "events.publish";
    public const string ManageNode = "events.calendars.manage";
    public const string OrganiseNode = "events.organise";
    public const string HaveNode = "events.calendar.have";
    public const string TurnedOffNode = "events.calendar.off";
    public const string DigestNode = "events.digest";

    public const string EditorRole = "events-calendar-editor";
    public const string PublisherRole = "events-calendar-publisher";
    public const string ManagerRole = "events-calendars-manager";
    public const string OrganiserRole = "events-organiser";
    public const string AdderRole = "events-calendar-adder";
    public const string TurnedOffRole = "events-calendar-turned-off";
    public const string DigestMakerRole = "events-digest-maker";
    public const string EveryCalendarEditorRole = "events-every-calendar-editor";
    public const string EveryCalendarPublisherRole = "events-every-calendar-publisher";

    public static readonly Permission Edit =
        new(EditNode, ScopeKind.Calendar, "Add and change events and regular events in a calendar", area: "Calendar", opensAdminArea: true);

    public static readonly Permission Publish =
        new(PublishNode, ScopeKind.Calendar, "Publish events in a calendar that needs a publisher", area: "Calendar");

    public static readonly Permission Manage =
        new(ManageNode, ScopeKind.Global, "Create calendars, choose who each is for and which are defaults, and keep the named breaks", area: "Calendar");

    public static readonly Permission Organise =
        new(OrganiseNode, ScopeKind.RegularEvent, "Organise a regular event: reminded when a date is missing what it needs", area: "Calendar");

    public static readonly Permission Have =
        new(HaveNode, ScopeKind.Calendar, "Has added a calendar to their week", area: "Calendar");

    public static readonly Permission TurnedOff =
        new(TurnedOffNode, ScopeKind.Calendar, "Has turned off a calendar they'd see anyway", area: "Calendar");

    public static readonly Permission Digest =
        new(DigestNode, ScopeKind.Global, "Make the weekly digest from the calendar, copy it for email and send it to the app", area: "Calendar", opensAdminArea: true);

    public static readonly IReadOnlyList<Permission> All = [Edit, Publish, Manage, Organise, Have, TurnedOff, Digest];
}
