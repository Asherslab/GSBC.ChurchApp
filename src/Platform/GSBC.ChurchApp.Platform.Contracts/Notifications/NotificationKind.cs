using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.Notifications;

public sealed record NotificationKind(
    string Key,
    string Description,
    string Area,
    bool DefaultPush = true,
    bool Mandatory = false,
    ScopeKind? PerScope = null,
    bool MayBeUrgent = false,
    NotificationPacing? Pacing = null,
    bool Waits = false,
    Permission? For = null,
    bool InAdminPanel = false,
    NotificationOpens? Opens = null,
    NotificationDigest? Digest = null);

public sealed record NotificationPacing(int Pushes, TimeSpan Per);

public sealed record NotificationDigest(string One, string Many, string Body, DigestSchedule Schedule, Permission? ChangedBy = null)
{
    public string Title(int count) => count == 1 ? One : string.Format(System.Globalization.CultureInfo.InvariantCulture, Many, count);
}

public sealed record DigestSchedule(IReadOnlyList<TimeSpan> After, TimeOnly? DailyAt)
{
    public static DigestSchedule Daily(TimeOnly at) => new([], at);

    public bool Equals(DigestSchedule? other) => other is not null && After.SequenceEqual(other.After) && DailyAt == other.DailyAt;

    public override int GetHashCode() => HashCode.Combine(After.Count, DailyAt);
}

public sealed record NotificationOpens(params IReadOnlyList<string> Routes)
{
    public static readonly NotificationOpens ChosenByStaff = new() { AnyPlace = true };

    private bool AnyPlace { get; init; }

    public bool Allows(string link) =>
        AnyPlace || (DestinationPaths.RouteOf(link) is { } route && Routes.Any(template => RouteTemplates.Match(template, route)));
}

public static class RouteTemplates
{
    public static bool Match(string template, string route)
    {
        string[] wanted = DestinationPaths.Trim(template.Split('?')[0]).Split('/');
        string[] given = DestinationPaths.Trim(route.Split('?')[0]).Split('/');
        return wanted.Length == given.Length && wanted.Zip(given).All(pair => IsParameter(pair.First) || IsParameter(pair.Second)
            ? pair.Second.Length > 0 && pair.First.Length > 0
            : string.Equals(pair.First, pair.Second, StringComparison.OrdinalIgnoreCase));
    }

    public static string Sample(string template) =>
        string.Join('/', template.Split('/').Select(segment => IsParameter(segment) ? "sample" : segment));

    private static bool IsParameter(string segment) => segment.StartsWith('{') && segment.EndsWith('}');
}
