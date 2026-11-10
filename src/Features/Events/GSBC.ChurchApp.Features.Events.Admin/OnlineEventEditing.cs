using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Events.Admin;

internal sealed class OnlineEventEditing(CalendarEditing editing) : IEventEditing
{
    public IReadOnlyList<string> Feeds { get; } = [EventsFeeds.Admin, EventsFeeds.Public, EventsFeeds.Private];

    public event Action? WaitingChanged
    {
        add { }
        remove { }
    }

    public async Task<CalendarDesk> DeskAsync() => await editing.DeskAsync();

    public async Task<EventDetail?> EventAsync(string calendarId, string eventId) =>
        await editing.Editing.EventAsync(new EventRef { CalendarId = calendarId, EventId = eventId });

    public async Task<RegularEventDetail?> RegularEventAsync(string calendarId, string regularEventId) =>
        await editing.Editing.RegularEventAsync(new RegularRef { CalendarId = calendarId, RegularEventId = regularEventId });

    public async Task<IReadOnlyList<RegularEventSummary>> RegularEventsAsync(string calendarId) =>
        (await editing.Editing.RegularEventsAsync(new CalendarRef { CalendarId = calendarId })).RegularEvents;

    public async Task<IReadOnlyList<EventPlace>> PlacesUsedAsync() => (await editing.PlacesUsedAsync()).Places;

    public async Task<BreakList> BreaksAsync() => await editing.BreaksAsync();

    public Task<IReadOnlyList<MediaRef>?> PicturesUsedAsync(string calendarId) => Task.FromResult<IReadOnlyList<MediaRef>?>(null);

    public async Task<ShowableList> ShowableAsync(string calendarId, string search) =>
        await editing.Editing.ShowableAsync(new ShowableQuery { CalendarId = calendarId, Search = search });

    public Task<EditSent> SaveEventAsync(EventSave save) => SendAsync(() => editing.Editing.SaveEventAsync(save));

    public Task<EditSent> SaveRegularEventAsync(RegularEventSave save) => SendAsync(() => editing.Editing.SaveRegularEventAsync(save));

    public Task<EditSent> SaveDateAsync(DateSave save) => SendAsync(() => editing.Editing.SaveDateAsync(save));

    public Task<EditSent> DeleteEventAsync(EventRef target) => SendAsync(() => editing.Editing.DeleteEventAsync(target));

    public Task<EditSent> DeleteRegularEventAsync(RegularRef target) => SendAsync(() => editing.Editing.DeleteRegularEventAsync(target));

    public Task<EditSent> AskToPublishAsync(ItemRef item) => SendAsync(() => editing.Editing.AskToPublishAsync(item));

    public Task<EditSent> ShowOnAsync(ShowOnRequest request) => SendAsync(() => editing.Editing.ShowOnAsync(request));

    public Task<EditSent> ShowHereAsync(ShowHereRequest request) => SendAsync(() => editing.Editing.ShowHereAsync(request));

    public Task<EditSent> TellPeopleAsync(TellPeople request) => SendAsync(() => editing.Editing.TellPeopleAsync(request));

    public EditWaiting? WaitingFor(string itemKey) => null;

    public Task RetryAsync(string itemKey) => Task.CompletedTask;

    public string Explain(Exception exception) => CalendarEditing.Problem(exception);

    private static async Task<EditSent> SendAsync<T>(Func<ValueTask<T>> send)
    {
        try
        {
            await send();
            return EditSent.Done;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new EditSent(CalendarEditing.Problem(exception));
        }
    }
}
