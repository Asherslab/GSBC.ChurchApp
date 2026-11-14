using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Events.Client;

public interface IEventEditing
{
    IReadOnlyList<string> Feeds { get; }

    event Action? WaitingChanged;

    Task<CalendarDesk> DeskAsync();

    Task<EventDetail?> EventAsync(string calendarId, string eventId);

    Task<RegularEventDetail?> RegularEventAsync(string calendarId, string regularEventId);

    Task<IReadOnlyList<RegularEventSummary>> RegularEventsAsync(string calendarId);

    Task<IReadOnlyList<EventPlace>> PlacesUsedAsync();

    Task<BreakList> BreaksAsync();

    Task<IReadOnlyList<MediaRef>?> PicturesUsedAsync(string calendarId);

    Task<ShowableList> ShowableAsync(string calendarId, string search);

    Task<EditSent> SaveEventAsync(EventSave save, IReadOnlyList<MediaRef>? files = null);

    Task<EditSent> SaveRegularEventAsync(RegularEventSave save, IReadOnlyList<MediaRef>? files = null);

    Task<EditSent> SaveDateAsync(DateSave save, IReadOnlyList<MediaRef>? files = null);

    Task<EditSent> DeleteEventAsync(EventRef target);

    Task<EditSent> DeleteRegularEventAsync(RegularRef target);

    Task<EditSent> AskToPublishAsync(ItemRef item);

    Task<EditSent> ShowOnAsync(ShowOnRequest request);

    Task<EditSent> ShowHereAsync(ShowHereRequest request);

    Task<EditSent> TellPeopleAsync(TellPeople request);

    EditWaiting? WaitingFor(string itemKey);

    Task RetryAsync(string itemKey);

    string Explain(Exception exception);
}

public sealed record EditSent(string? Problem = null, bool Queued = false)
{
    public static readonly EditSent Done = new();

    public static readonly EditSent Waiting = new(Queued: true);

    public bool Succeeded => Problem is null;
}

public sealed record EditWaiting(SaveState State, string? Problem)
{
    public const string WaitingWords = "Waiting to send";
    public const string FailedWords = "Couldn't save.";
}

public static class EditKeys
{
    public static string Event(string eventId) => $"event/{eventId}";

    public static string Regular(string regularEventId) => $"regular/{regularEventId}";

    public static string Date(string regularEventId, string date) => $"regular/{regularEventId}/{date}";

    public static string? OfOccurrence(string occurrenceId) =>
        EventDates.TryReadOccurrenceId(occurrenceId, out Guid itemId, out DateOnly? date)
            ? date is { } day ? Date(itemId.ToString(), EventDates.Format(day)) : Event(itemId.ToString())
            : null;
}
