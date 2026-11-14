namespace GSBC.ChurchApp.Platform.Client.Location;

public enum LocationStart
{
    Running,
    Denied,
    Off,
}

public enum LocationAuthorization
{
    NotDetermined,
    Denied,
    WhenInUse,
    Always,
    Off,
}

public sealed record LocationStatus(bool Running, LocationAuthorization Authorization);

public sealed record LocationFix(long Seq, DateTimeOffset At, double Lat, double Lon, double Accuracy, double Heading, double Speed);

public sealed record LocationFixes(IReadOnlyList<LocationFix> Fixes, long Latest, bool Running);

public interface ILocationSession
{
    bool IsAvailable { get; }

    event Action? FixArrived;

    Task<LocationStart> StartAsync(string purpose);

    Task StopAsync();

    Task<LocationStatus> StateAsync();

    Task<LocationFixes> FixesAsync(long since);

    Task ClearAsync(long upTo);
}
