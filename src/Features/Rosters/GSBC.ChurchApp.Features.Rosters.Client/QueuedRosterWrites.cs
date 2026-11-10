using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Client.Outbox;

namespace GSBC.ChurchApp.Features.Rosters.Client;

public sealed class QueuedRosterWrites(IOutbox outbox) : IRosterWrites
{
    public bool Queued => true;

    public Task SavePositionAsync(string ministryId, Position position) =>
        SendAsync(RosterMethods.SavePosition, new PositionSave { MinistryId = ministryId, Position = RosterIds.Minted(position) });

    public Task DeletePositionAsync(string ministryId, string positionId) => SendAsync(RosterMethods.DeletePosition, new ItemRef { MinistryId = ministryId, Id = positionId });

    public Task SetTrainedAsync(string ministryId, string positionId, string personId, bool trained) =>
        SendAsync(RosterMethods.SetTrained, new TrainedChange { MinistryId = ministryId, PositionId = positionId, PersonId = personId, Trained = trained });

    public Task SaveSettingsAsync(string ministryId, RosterSettings settings) =>
        SendAsync(RosterMethods.SaveSettings, new SettingsSave { MinistryId = ministryId, Settings = settings });

    public Task AddDateAsync(string ministryId, string eventKey) =>
        SendAsync(RosterMethods.AddDate, new DateAdd { MinistryId = ministryId, DateId = RosterIds.New(), EventKey = eventKey });

    public Task RemoveDateAsync(string ministryId, string dateId) => SendAsync(RosterMethods.RemoveDate, new ItemRef { MinistryId = ministryId, Id = dateId });

    public Task AssignAsync(string ministryId, string dateId, string positionId, string personId) =>
        SendAsync(RosterMethods.Assign, new AssignRequest { MinistryId = ministryId, AssignmentId = RosterIds.New(), DateId = dateId, PositionId = positionId, PersonId = personId });

    public Task UnassignAsync(string ministryId, string assignmentId) => SendAsync(RosterMethods.Unassign, new ItemRef { MinistryId = ministryId, Id = assignmentId });

    public Task DecideSwapAsync(string ministryId, string swapId, bool yes) =>
        SendAsync(RosterMethods.DecideSwap, new SwapDecision { MinistryId = ministryId, SwapId = swapId, Yes = yes });

    public Task ReplyAsync(string ministryId, string assignmentId, bool accept, string reason) =>
        SendAsync(RosterMethods.Reply, new ReplyRequest { MinistryId = ministryId, AssignmentId = assignmentId, Accept = accept, Reason = reason });

    public Task AskSwapAsync(string ministryId, string assignmentId, string way, string toPersonId, string note) =>
        SendAsync(RosterMethods.AskSwap, new SwapAsk { MinistryId = ministryId, SwapId = RosterIds.New(), AssignmentId = assignmentId, Way = way, ToPersonId = toPersonId, Note = note });

    public Task TakeSwapAsync(string ministryId, string swapId, string personId, bool yes) =>
        SendAsync(RosterMethods.TakeSwap, new SwapTake { MinistryId = ministryId, SwapId = swapId, PersonId = personId, Yes = yes });

    public Task WithdrawSwapAsync(string ministryId, string swapId) => SendAsync(RosterMethods.WithdrawSwap, new SwapRef { MinistryId = ministryId, SwapId = swapId });

    public Task SaveAwayAsync(AwaySave away)
    {
        if (away.AwayId.Length == 0)
            away.AwayId = RosterIds.New();
        return SendAsync(RosterMethods.SaveAway, away);
    }

    public Task RemoveAwayAsync(string awayId) => SendAsync(RosterMethods.RemoveAway, new AwayRef { AwayId = awayId });

    private async Task SendAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string method, T request) =>
        await outbox.EnqueueAsync(method, request, RostersContractsModel.Instance);
}
