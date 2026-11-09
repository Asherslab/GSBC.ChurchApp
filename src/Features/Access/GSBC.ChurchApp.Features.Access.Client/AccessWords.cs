using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Client;

public static class AccessWords
{
    public static string Place(string? placeName, bool includesTeams) =>
        placeName is null ? "Everywhere" : includesTeams ? $"In {placeName} and its teams" : $"In {placeName}";

    public static string PlaceOf(string objectId, string? name, string scopeKind, bool includesTeams) =>
        name is null || name == objectId
            ? includesTeams ? $"For one {scopeKind}, and its teams" : $"For one {scopeKind}"
            : scopeKind == ScopeKind.Draft.Name ? $"For the draft “{name}”" : Place(name, includesTeams);

    public static string OneThing(string roleName, string scopeKind, string? placeName) =>
        (scopeKind, placeName) switch
        {
            (_, null) => $"{roleName} of a {scopeKind.Replace('-', ' ')}",
            ("calendar", _) => $"{roleName} of the {placeName} calendar",
            ("draft", _) => $"{roleName} of the draft “{placeName}”",
            _ => $"{roleName} of {placeName}",
        };

    public static string And(IEnumerable<string> names)
    {
        List<string> all = names.ToList();
        return all.Count <= 1 ? string.Concat(all) : $"{string.Join(", ", all[..^1])} and {all[^1]}";
    }

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

    public static string? Added(AssignmentInfo? assignment, TimeProvider time) =>
        assignment is null ? null
        : assignment.CreatedBy is { } by ? $"Added by {by}, {When(assignment.CreatedAt, time)}"
        : $"Added {When(assignment.CreatedAt, time)}";

    private static string When(DateTime at, TimeProvider time) =>
        at.ToLocalTime().ToString(at.Year == time.GetLocalNow().Year ? "d MMM" : "d MMM yyyy");

    public static string Until(DateTime? expiresAt) =>
        expiresAt is { } ends ? $"until {ends.ToLocalTime():d MMM yyyy}" : "";

    public const string Sensitive = "Needs extra care: people with this can't reset their sign-in by email.";

    public const string Nothing = "Nothing beyond what everyone can do.";
}
