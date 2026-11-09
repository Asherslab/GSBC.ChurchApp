using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Scopes;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Events.Admin;

internal sealed class CalendarScopeNames(CalendarEditing editing) : IScopeNamer
{
    public ScopeKind Kind => ScopeKind.Calendar;

    public IReadOnlyCollection<string> Feeds { get; } = [EventsFeeds.Calendars, EventsFeeds.Admin];

    public async Task<IReadOnlyList<ScopeChoice>> ChoicesAsync()
    {
        try
        {
            return (await editing.DeskAsync()).Calendars.Select(calendar => new ScopeChoice(calendar.Settings.Calendar.Id, calendar.Settings.Calendar.Name, CalendarWorkbenchLinks.Calendar(calendar.Settings.Calendar.Slug))).ToList();
        }
        catch (RpcException)
        {
            return [];
        }
    }
}
