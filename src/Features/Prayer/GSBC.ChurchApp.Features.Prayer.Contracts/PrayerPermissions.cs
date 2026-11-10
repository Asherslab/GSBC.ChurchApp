using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Prayer.Contracts;

public static class PrayerPermissions
{
    public const string ReadNode = "prayer.requests.read";
    public const string WallNode = "prayer.wall.moderate";

    public const string PastoralTeamRole = "pastoral-team";
    public const string WallRole = "prayer-wall";

    public static readonly Permission Read =
        new(ReadNode, ScopeKind.Global, "Read every prayer request, keep notes on them, follow them up and delete them", isSensitive: true, area: "Prayer");

    public static readonly Permission Wall =
        new(WallNode, ScopeKind.Global, "Read the prayer requests that ask for the prayer wall, and choose what the wall shows", area: "Prayer");

    public static readonly IReadOnlyList<Permission> All = [Read, Wall];
}
