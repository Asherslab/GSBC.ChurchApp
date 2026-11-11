namespace GSBC.ChurchApp.Platform.Api.Calendars;

public interface IRosterableEvents
{
    ValueTask<IReadOnlyList<RosterableEvent>> VisibleToAsync(Guid personId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    ValueTask<IReadOnlyDictionary<string, RosterableEvent>> FindAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken);
}

public sealed record RosterableEvent(string Key, string Title, Guid CalendarId, string CalendarName, DateTime StartsAt, DateTime EndsAt, bool AllDay, bool NotOn);

internal sealed class NoRosterableEvents : IRosterableEvents
{
    public ValueTask<IReadOnlyList<RosterableEvent>> VisibleToAsync(Guid personId, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<RosterableEvent>>([]);

    public ValueTask<IReadOnlyDictionary<string, RosterableEvent>> FindAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<string, RosterableEvent>>(new Dictionary<string, RosterableEvent>());
}
