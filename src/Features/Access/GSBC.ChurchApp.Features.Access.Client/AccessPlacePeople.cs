using Grpc.Core;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Client.People;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Platform.Client.Online;

namespace GSBC.ChurchApp.Features.Access.Client;

[AdminOnly]
internal sealed class AccessPlacePeopleSource(AccessClient access, TimeProvider time) : IPlacePeopleSource
{
    public IPlacePeople For(ScopeKind kind, string objectId) => new AccessPlacePeople(access, time, kind, objectId);
}

[AdminOnly]
internal sealed class AccessPlacePeople(AccessClient access, TimeProvider time, ScopeKind kind, string objectId) : IPlacePeople
{
    private PlacePeople? _last;

    public IReadOnlyCollection<string> Feeds { get; } = [AccessFeeds.Roles, AccessFeeds.Grants];

    public async Task<PlacePeopleView> LoadAsync()
    {
        try
        {
            _last = await access.ListPlaceAsync(kind.Name, objectId);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.PermissionDenied)
        {
            _last = null;
            return PlacePeopleView.NotYours;
        }

        return new PlacePeopleView(
            _last.Roles.Select(role => RoleAbilities.View(role, _last.Abilities)).ToList(),
            _last.People.Select(person => new PlacePersonView(
                person.UserId,
                person.Name,
                AccessWords.Added(person.Assignments.MinBy(assignment => assignment.CreatedAt), time),
                person.Assignments.Select(assignment => assignment.RoleId).ToHashSet())).ToList());
    }

    public async Task<IReadOnlyList<PersonChoice>> FindAsync(string query) =>
        (await access.FindPeopleAsync(kind.Name, objectId, query)).People
            .Select(person => new PersonChoice(person.Id, person.Name, person.Email))
            .ToList();

    public async Task GiveAsync(string userId, string roleId) =>
        await access.AssignAsync(new AssignRequest { UserId = userId, RoleId = roleId, ScopeObjectId = objectId });

    public async Task TakeAwayAsync(string userId, string roleId)
    {
        if (_last?.People.FirstOrDefault(person => person.UserId == userId)?.Assignments.FirstOrDefault(assignment => assignment.RoleId == roleId) is { } held)
            await access.UnassignAsync(held.Id);
    }
}
