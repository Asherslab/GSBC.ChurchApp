using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Events.Client;

public interface IYourCalendarLink
{
    Task<string?> PersonalKeyAsync();

    Task<string?> ResetAsync();
}

public sealed class PhoneCalendarLink(ChannelBase channel) : IYourCalendarLink
{
    private readonly ICalendarFilesService _files = channel.CreateGrpcService<ICalendarFilesService>();
    private string? _key;

    public async Task<string?> PersonalKeyAsync() => _key ??= await KeyOfAsync(() => _files.LinkAsync(new Done()));

    public async Task<string?> ResetAsync() => _key = await KeyOfAsync(() => _files.ResetLinkAsync(new Done())) ?? _key;

    internal static async Task<string?> KeyOfAsync(Func<ValueTask<CalendarLink>> ask)
    {
        try
        {
            return (await ask()).Key is { Length: > 0 } key ? key : null;
        }
        catch (RpcException)
        {
            return null;
        }
    }
}

public sealed class CalendarSubscriptions(ChannelBase channel, IYourCalendarLink yours)
{
    private Uri Origin => new(channel.Target.Contains("://", StringComparison.Ordinal) ? channel.Target : $"https://{channel.Target}");

    public string Web(string path) => new Uri(Origin, path).ToString();

    public string Subscribe(string path) => CalendarLinks.Subscribe(Origin, path);

    public static string CalendarPath(CalendarInfo calendar) => CalendarLinks.CalendarPath(calendar.Slug);

    public static string PublicPath => CalendarLinks.PublicPath;

    public static string MinePath(string key) => CalendarLinks.MinePath(key);

    public static string MinePath(string key, CalendarInfo calendar) => CalendarLinks.MinePath(key, calendar.Slug);

    public Task<string?> PersonalKeyAsync() => yours.PersonalKeyAsync();

    public Task<string?> ResetAsync() => yours.ResetAsync();

    public static bool IsForEveryone(CalendarInfo calendar) => calendar.Audience.Length == 0 || calendar.Audience == GSBC.ChurchApp.Platform.Contracts.Configuration.Audience.Everyone.Text;
}
