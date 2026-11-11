using GSBC.ChurchApp.Features.Rosters.Contracts;

namespace GSBC.ChurchApp.Features.Rosters.Client;

public static class RosterIds
{
    public static string New() => Guid.CreateVersion7().ToString();

    public static Position Minted(Position position)
    {
        Position sent = RostersContractsModel.Instance.DeepClone(position);
        if (sent.Id.Length == 0)
            (sent.Id, sent.New) = (New(), true);
        return sent;
    }
}
