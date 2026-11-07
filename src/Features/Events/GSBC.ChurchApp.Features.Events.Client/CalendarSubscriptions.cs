using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client;

public interface IYourCalendarLink
{
    Task<string?> SecretAsync();
}

public sealed class NoCalendarLinkYet : IYourCalendarLink
{
    public Task<string?> SecretAsync() => Task.FromResult<string?>(null);
}

public sealed class CalendarSubscriptions(ChannelBase channel, IYourCalendarLink yours)
{
    private string Host => channel.Target.Contains("://", StringComparison.Ordinal) ? new Uri(channel.Target).Authority : channel.Target;

    public string Web(string path) => $"https://{Host}{path}";

    public string Subscribe(string path) => $"webcal://{Host}{path}";

    public static string CalendarPath(CalendarInfo calendar) => $"/calendars/{calendar.Slug}.ics";

    public static string PublicPath => $"/calendars/{CalendarLinks.Public}.ics";

    public static string MinePath(string secret) => $"/calendars/{CalendarLinks.Mine}/{secret}.ics";

    public static string MinePath(string secret, CalendarInfo calendar) => $"/calendars/{CalendarLinks.Mine}/{secret}/{calendar.Slug}.ics";

    public Task<string?> SecretAsync() => yours.SecretAsync();

    public static bool IsForEveryone(CalendarInfo calendar) => calendar.Audience.Length == 0 || calendar.Audience == GSBC.ChurchApp.Platform.Contracts.Configuration.Audience.Everyone.Text;
}
