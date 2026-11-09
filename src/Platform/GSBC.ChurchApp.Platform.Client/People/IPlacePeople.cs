using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Client.People;

public interface IPlacePeople
{
    IReadOnlyCollection<string> Feeds { get; }

    Task<PlacePeopleView> LoadAsync();

    Task<IReadOnlyList<PersonChoice>> FindAsync(string query);

    Task GiveAsync(string userId, string roleId);

    Task TakeAwayAsync(string userId, string roleId);
}

public interface IPlacePeopleSource
{
    IPlacePeople For(ScopeKind kind, string objectId);
}

public sealed record PlacePeopleView(IReadOnlyList<PlaceRoleView> Roles, IReadOnlyList<PlacePersonView> People)
{
    public static readonly PlacePeopleView NotYours = new([], []);

    public bool MayAdd => Roles.Any(role => role.Refusal is null);
}

public sealed record PlaceRoleView(string Id, string Name, string Description, IReadOnlyList<AbilityGroup> Gives, string? Refusal);

public sealed record AbilityGroup(string Area, IReadOnlyList<string> Abilities, bool BecauseTheJobNeedsIt = false);

public sealed record PlacePersonView(string Id, string Name, string? Detail, IReadOnlyCollection<string> RoleIds);

public sealed record PersonChoice(string Id, string Name, string Detail);
