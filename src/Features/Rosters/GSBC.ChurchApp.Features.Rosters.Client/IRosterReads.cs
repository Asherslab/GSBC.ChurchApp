using GSBC.ChurchApp.Features.Rosters.Contracts;

namespace GSBC.ChurchApp.Features.Rosters.Client;

public interface IRosterReads
{
    IReadOnlyList<string> Feeds { get; }

    Task<RosterView> RosterAsync(string ministryId);

    Task<IReadOnlyList<RosterView>> YoursAsync();

    Task<AwayList> AwayAsync();

    Task<IReadOnlyList<EventChoice>> EventChoicesAsync(string ministryId);
}

public sealed class RosterNotHereException() : Exception("The roster isn't on this phone yet.");
