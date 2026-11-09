using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Access;

public sealed record SystemRole(
    string Key,
    string Name,
    string Description,
    ScopeKind ScopeKind,
    IReadOnlyList<string> Patterns,
    IReadOnlyList<string>? AssignableBy = null,
    bool AppliesToChildren = false,
    bool PhoneHoldable = false,
    string? Feature = null,
    bool Hidden = false,
    IReadOnlyList<string>? Kits = null,
    IReadOnlyList<string>? Replaces = null,
    IReadOnlyList<string>? StartsWithHoldersOf = null);

public sealed record SystemRolePatterns(string RoleKey, IReadOnlyList<string> Patterns);

public sealed record RoleKit(string Name, IReadOnlyList<string> Patterns);

public static class RoleKits
{
    public const string Pictures = "pictures";
    public const string PeopleNames = "people-names";
}
