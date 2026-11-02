using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Access.Contracts;

[Service("gsbc.access.Access")]
public interface IAccessService
{
    [SignedIn]
    ValueTask<GrantList> GetMineAsync(Nothing request, CallContext context = default);

    [Requires(AccessPermissions.ViewNode)]
    ValueTask<Catalog> ListCatalogAsync(Nothing request, CallContext context = default);

    [Requires(AccessPermissions.ViewNode)]
    ValueTask<RoleList> ListRolesAsync(Nothing request, CallContext context = default);

    [Requires(AccessPermissions.EditRolesNode)]
    ValueTask<RoleInfo> SaveRoleAsync(RoleInfo request, CallContext context = default);

    [Requires(AccessPermissions.EditRolesNode)]
    ValueTask<RoleEffect> PreviewRoleAsync(RolePreviewRequest request, CallContext context = default);

    [Requires(AccessPermissions.EditRolesNode)]
    ValueTask<Done> DeleteRoleAsync(RoleReference request, CallContext context = default);

    [Requires(AccessPermissions.ViewNode)]
    ValueTask<AssignmentList> ListAssignmentsAsync(AssignmentQuery request, CallContext context = default);

    [Requires(AccessPermissions.ViewNode)]
    ValueTask<PersonAbilities> ExplainAsync(PersonReference request, CallContext context = default);

    [Requires(AccessPermissions.EditAssignmentsNode)]
    ValueTask<AssignmentInfo> AssignAsync(AssignRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> UnassignAsync(AssignmentReference request, CallContext context = default);
}
