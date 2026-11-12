namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record EventChoiceLine(string Key, string Title, string CalendarName, string? MinistryId, DateTime StartsAt, DateTime EndsAt, bool AllDay, string CalendarId = "");

public sealed record CalendarChoiceLine(string Id, string Name, string? MinistryId);

public interface IEventChoices
{
    IReadOnlyList<string> Feeds { get; }

    Task<IReadOnlyList<EventChoiceLine>> UpcomingAsync(DateTime fromUtc, DateTime toUtc);

    Task<IReadOnlyList<CalendarChoiceLine>> ForEveryoneAsync() => Task.FromResult<IReadOnlyList<CalendarChoiceLine>>([]);
}
