using Grpc.Core;
using GSBC.ChurchApp.Features.Access.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Access.Client;

public sealed class AccessClient(ChannelBase channel)
{
    private readonly IAccessService _access = channel.CreateGrpcService<IAccessService>();

    public ValueTask<GrantList> GetMineAsync() => _access.GetMineAsync(new Nothing());

    public ValueTask<Catalog> ListCatalogAsync() => _access.ListCatalogAsync(new Nothing());

    public ValueTask<RoleNameList> ListRoleNamesAsync() => _access.ListRoleNamesAsync(new Nothing());

    public ValueTask<RoleList> ListRolesAsync() => _access.ListRolesAsync(new Nothing());

    public ValueTask<AssignmentList> ListAssignmentsAsync(string? userId) => _access.ListAssignmentsAsync(new AssignmentQuery { UserId = userId });

    public ValueTask<PersonAbilities> ExplainAsync(string userId) => _access.ExplainAsync(new PersonReference { UserId = userId });

    public ValueTask<AssignmentInfo> AssignAsync(AssignRequest request) => _access.AssignAsync(request);

    public ValueTask<Done> UnassignAsync(string id) => _access.UnassignAsync(new AssignmentReference { Id = id });

    public ValueTask<PlacePeople> ListPlaceAsync(string scopeKind, string? objectId) =>
        _access.ListPlaceAsync(new PlaceReference { ScopeKind = scopeKind, ObjectId = objectId });

    public ValueTask<FoundPeople> FindPeopleAsync(string scopeKind, string? objectId, string query) =>
        _access.FindPeopleAsync(new FindPeopleRequest { Place = new PlaceReference { ScopeKind = scopeKind, ObjectId = objectId }, Query = query });
}
