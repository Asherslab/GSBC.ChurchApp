using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Client;

public static class AccessWords
{
    public static string Place(string? placeName, bool includesTeams) =>
        placeName is null ? "Everywhere" : includesTeams ? $"In {placeName} and its teams" : $"In {placeName}";

    public static string GivenWhere(RoleInfo role) =>
        role.ScopeKind == ScopeKind.Global.Name ? "Everywhere"
        : role.AppliesToChildren ? $"For one {role.ScopeKind}, and its teams"
        : $"For one {role.ScopeKind}";

    public static string GivenBy(RoleInfo role, IReadOnlyList<RoleInfo> roles)
    {
        List<string> names = role.AssignableByRoleIds
            .Select(id => roles.FirstOrDefault(other => other.Id == id)?.Name)
            .OfType<string>()
            .Order()
            .ToList();
        return names.Count == 0 ? "People who manage access" : $"{string.Join(", ", names)}, and people who manage access";
    }

    public static string Holders(int count) => count switch
    {
        0 => "Nobody yet",
        1 => "1 person",
        _ => $"{count} people",
    };

    public static string Until(DateTime? expiresAt) =>
        expiresAt is { } ends ? $"until {ends.ToLocalTime():d MMM yyyy}" : "";

    public const string Sensitive = "Needs extra care: people with this can't reset their sign-in by email.";

    public const string Nothing = "Nothing beyond what everyone can do.";
}
