using Grpc.Core;
using GSBC.ChurchApp.Features.Rosters.Client;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Rosters.Admin;

public sealed class OnlineRosters(ChannelBase channel) : IRosterReads
{
    private readonly IRostersService _rosters = channel.CreateGrpcService<IRostersService>();

    public IReadOnlyList<string> Feeds { get; } = [RostersFeeds.Changes, RostersFeeds.Ministries, RostersFeeds.Away];

    public async Task<RosterView> RosterAsync(string ministryId) => await _rosters.RosterAsync(new RosterQuery { MinistryId = ministryId });

    public Task<IReadOnlyList<RosterView>> YoursAsync() => Task.FromResult<IReadOnlyList<RosterView>>([]);

    public async Task<AwayList> AwayAsync() => await _rosters.AwayAsync(new Nothing());

    public async Task<IReadOnlyList<EventChoice>> EventChoicesAsync(string ministryId) =>
        (await _rosters.EventChoicesAsync(new RosterQuery { MinistryId = ministryId })).Events;
}
