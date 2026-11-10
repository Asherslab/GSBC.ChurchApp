using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.People.Contracts;

[Service("gsbc.people.SafeMinistryCheck")]
public interface ISafeMinistryCheckService
{
    [Requires(PeoplePermissions.SafeguardingViewNode)]
    ValueTask<SmcSyncStatus> GetStatusAsync(SmcStatusRequest request, CallContext context = default);

    [Requires(PeoplePermissions.SafeguardingEditNode)]
    ValueTask<SmcSyncStatus> SyncNowAsync(SmcSyncRequest request, CallContext context = default);

    [Requires(PeoplePermissions.SafeguardingViewNode)]
    ValueTask<SmcPeopleList> GetUnmatchedAsync(SmcUnmatchedRequest request, CallContext context = default);

    [Requires(PeoplePermissions.SafeguardingEditNode)]
    ValueTask<Done> LinkAsync(SmcLinkRequest request, CallContext context = default);

    [Requires(PeoplePermissions.SafeguardingEditNode)]
    ValueTask<Done> UnlinkAsync(PersonRequest request, CallContext context = default);
}
