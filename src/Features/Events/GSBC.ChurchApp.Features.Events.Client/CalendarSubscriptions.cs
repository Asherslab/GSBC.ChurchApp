using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client;

public interface IYourCalendarLink
{
    Task<string?> PersonalKeyAsync();

    Task<string?> ResetAsync();
}

public sealed class NoCalendarLinkYet : IYourCalendarLink
{
    public Task<string?> PersonalKeyAsync() => Task.FromResult<string?>(null);

    public Task<string?> ResetAsync() => Task.FromResult<string?>(null);
}

public sealed class CalendarSubscriptions(ChannelBase channel, IYourCalendarLink yours)
{
    private string Host => channel.Target.Contains("://", StringComparison.Ordinal) ? new Uri(channel.Target).Authority : channel.Target;

    public string Web(string path) => $"https://{Host}{path}";

    public string Subscribe(string path) => $"webcal://{Host}{path}";

    public static string CalendarPath(CalendarInfo calendar) => $"/calendars/{calendar.Slug}.ics";

    public static string PublicPath => $"/calendars/{CalendarLinks.Public}.ics";

    public static string MinePath(string key) => $"/calendars/{CalendarLinks.Mine}/{key}.ics";

    public static string MinePath(string key, CalendarInfo calendar) => $"/calendars/{CalendarLinks.Mine}/{key}/{calendar.Slug}.ics";

    public Task<string?> PersonalKeyAsync() => yours.PersonalKeyAsync();

    public Task<string?> ResetAsync() => yours.ResetAsync();

    public static bool IsForEveryone(CalendarInfo calendar) => calendar.Audience.Length == 0 || calendar.Audience == GSBC.ChurchApp.Platform.Contracts.Configuration.Audience.Everyone.Text;
}
