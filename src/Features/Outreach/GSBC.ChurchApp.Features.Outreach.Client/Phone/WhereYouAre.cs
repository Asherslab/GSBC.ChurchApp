using GSBC.ChurchApp.Platform.Client.Location;
using RoutingPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public enum HereState
{
    Found,
    Denied,
    Off,
    NotFound,
}

public sealed record Here(HereState State, RoutingPoint? Point = null, double Accuracy = 0);

public sealed class WhereYouAre(ILocationSession location, WalkSession walk, TimeProvider time)
{
    private const string Purpose = "outreach-here";
    private const double GoodEnoughMetres = 60;
    private static readonly TimeSpan Longest = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Fresh = TimeSpan.FromMinutes(2);
    private Here? _last;
    private DateTimeOffset _lastAt;

    public bool CanAsk => location.IsAvailable;

    public async Task<Here> FindAsync(bool again = false)
    {
        if (walk.Progress?.You is { } walking)
            return new Here(HereState.Found, walking, walk.Progress.Accuracy);
        if (!again && _last is { State: HereState.Found } last && time.GetUtcNow() - _lastAt < Fresh)
            return last;
        if (!location.IsAvailable)
            return new Here(HereState.Off);
        long since = (await location.FixesAsync(long.MaxValue)).Latest;
        LocationStart started = await location.StartAsync(Purpose);
        if (started != LocationStart.Running)
            return new Here(started == LocationStart.Denied ? HereState.Denied : HereState.Off);
        try
        {
            DateTimeOffset until = time.GetUtcNow() + Longest;
            LocationFix? best = null;
            while (time.GetUtcNow() < until)
            {
                LocationFixes fixes = await location.FixesAsync(since);
                foreach (LocationFix fix in fixes.Fixes.Where(fix => best is null || fix.Accuracy < best.Accuracy))
                    best = fix;
                if (best is { Accuracy: <= GoodEnoughMetres })
                    break;
                await Task.Delay(TimeSpan.FromMilliseconds(400));
            }
            _last = best is null ? new Here(HereState.NotFound) : new Here(HereState.Found, new RoutingPoint(best.Lat, best.Lon), best.Accuracy);
            _lastAt = time.GetUtcNow();
            return _last;
        }
        finally
        {
            if (walk.Current is not { Finished: false })
                await location.StopAsync();
        }
    }
}
