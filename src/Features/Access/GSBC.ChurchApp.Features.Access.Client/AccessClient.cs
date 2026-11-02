using Grpc.Core;
using GSBC.ChurchApp.Features.Access.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Access.Client;

public sealed class AccessClient(ChannelBase channel)
{
    private readonly IAccessService _access = channel.CreateGrpcService<IAccessService>();

    public ValueTask<GrantList> GetMineAsync() => _access.GetMineAsync(new Nothing());

    public ValueTask<Catalog> ListCatalogAsync() => _access.ListCatalogAsync(new Nothing());

    public ValueTask<RoleList> ListRolesAsync() => _access.ListRolesAsync(new Nothing());

    public ValueTask<RoleInfo> SaveRoleAsync(RoleInfo role) => _access.SaveRoleAsync(role);

    public ValueTask<RoleEffect> PreviewRoleAsync(RoleInfo role, bool deleting = false) =>
        _access.PreviewRoleAsync(new RolePreviewRequest { Role = role, Deleting = deleting });

    public ValueTask<Done> DeleteRoleAsync(string id) => _access.DeleteRoleAsync(new RoleReference { Id = id });

    public ValueTask<AssignmentList> ListAssignmentsAsync(string? userId) => _access.ListAssignmentsAsync(new AssignmentQuery { UserId = userId });

    public ValueTask<PersonAbilities> ExplainAsync(string userId) => _access.ExplainAsync(new PersonReference { UserId = userId });

    public ValueTask<AssignmentInfo> AssignAsync(AssignRequest request) => _access.AssignAsync(request);

    public ValueTask<Done> UnassignAsync(string id) => _access.UnassignAsync(new AssignmentReference { Id = id });
}
