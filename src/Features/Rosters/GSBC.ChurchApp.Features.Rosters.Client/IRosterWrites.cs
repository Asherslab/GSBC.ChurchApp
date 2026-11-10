using GSBC.ChurchApp.Features.Rosters.Contracts;

namespace GSBC.ChurchApp.Features.Rosters.Client;

public interface IRosterWrites
{
    bool Queued { get; }

    Task SavePositionAsync(string ministryId, Position position);

    Task DeletePositionAsync(string ministryId, string positionId);

    Task SetTrainedAsync(string ministryId, string positionId, string personId, bool trained);

    Task SaveSettingsAsync(string ministryId, RosterSettings settings);

    Task AddDateAsync(string ministryId, string eventKey);

    Task RemoveDateAsync(string ministryId, string dateId);

    Task AssignAsync(string ministryId, string dateId, string positionId, string personId);

    Task UnassignAsync(string ministryId, string assignmentId);

    Task DecideSwapAsync(string ministryId, string swapId, bool yes);

    Task ReplyAsync(string ministryId, string assignmentId, bool accept, string reason);

    Task AskSwapAsync(string ministryId, string assignmentId, string way, string toPersonId, string note);

    Task TakeSwapAsync(string ministryId, string swapId, string personId, bool yes);

    Task WithdrawSwapAsync(string ministryId, string swapId);

    Task SaveAwayAsync(AwaySave away);

    Task RemoveAwayAsync(string awayId);
}

public sealed class RosterRefusedException(string message) : Exception(message);
