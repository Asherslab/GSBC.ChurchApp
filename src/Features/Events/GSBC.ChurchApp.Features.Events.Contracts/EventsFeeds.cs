namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class EventsFeeds
{
    public const string Calendars = "events.calendars";
    public const string PrivateCalendars = "events.calendars.private";
    public const string Public = "events.public";
    public const string Private = "events.private";
    public const string Contacts = "events.contacts";
    public const string Admin = "events.admin";
    public const string Digest = "events.digest";
    public const string PrivateDigest = "events.digest.private";
    public const string Mine = "events.mine";

    public const int DaysBack = 30;
    public const int MonthsAhead = 12;
    public const int NotOnShownForDays = 14;

    public static readonly TimeSpan ChangedFor = TimeSpan.FromDays(7);
}
