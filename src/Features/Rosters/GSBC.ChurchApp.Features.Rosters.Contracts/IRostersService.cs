using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Rosters.Contracts;

[Service("gsbc.rosters.Rosters")]
public interface IRostersService
{
    [SignedIn]
    ValueTask<RosterView> RosterAsync(RosterQuery request, CallContext context = default);

    [Requires(RostersPermissions.EditNode)]
    ValueTask<EventChoices> EventChoicesAsync(RosterQuery request, CallContext context = default);

    [SignedIn]
    ValueTask<AwayList> AwayAsync(Nothing request, CallContext context = default);

    [Requires(RostersPermissions.EditNode)]
    ValueTask<Done> SavePositionAsync(PositionSave request, CallContext context = default);

    [Requires(RostersPermissions.EditNode)]
    ValueTask<Done> DeletePositionAsync(ItemRef request, CallContext context = default);

    [Requires(RostersPermissions.EditNode)]
    ValueTask<Done> SetTrainedAsync(TrainedChange request, CallContext context = default);

    [Requires(RostersPermissions.EditNode)]
    ValueTask<Done> SaveSettingsAsync(SettingsSave request, CallContext context = default);

    [Requires(RostersPermissions.EditNode)]
    ValueTask<Done> AddDateAsync(DateAdd request, CallContext context = default);

    [Requires(RostersPermissions.EditNode)]
    ValueTask<Done> RemoveDateAsync(ItemRef request, CallContext context = default);

    [Requires(RostersPermissions.EditNode)]
    ValueTask<Done> AssignAsync(AssignRequest request, CallContext context = default);

    [Requires(RostersPermissions.EditNode)]
    ValueTask<Done> UnassignAsync(ItemRef request, CallContext context = default);

    [Requires(RostersPermissions.EditNode)]
    ValueTask<Done> DecideSwapAsync(SwapDecision request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> ReplyAsync(ReplyRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> AskSwapAsync(SwapAsk request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> TakeSwapAsync(SwapTake request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> WithdrawSwapAsync(SwapRef request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> SaveAwayAsync(AwaySave request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> RemoveAwayAsync(AwayRef request, CallContext context = default);
}
