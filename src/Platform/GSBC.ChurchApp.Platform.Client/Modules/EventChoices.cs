namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record EventChoiceLine(string Key, string Title, string CalendarName, string? MinistryId, DateTime StartsAt, DateTime EndsAt, bool AllDay);

public interface IEventChoices
{
    IReadOnlyList<string> Feeds { get; }

    Task<IReadOnlyList<EventChoiceLine>> UpcomingAsync(DateTime fromUtc, DateTime toUtc);
}
