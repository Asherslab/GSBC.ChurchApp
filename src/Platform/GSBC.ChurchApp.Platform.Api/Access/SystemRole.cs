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
    bool PhoneHoldable = false);

public sealed record SystemRolePatterns(string RoleKey, IReadOnlyList<string> Patterns);
