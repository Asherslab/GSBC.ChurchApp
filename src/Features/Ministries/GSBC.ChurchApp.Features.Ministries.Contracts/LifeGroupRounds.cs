namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class LifeGroupRounds
{
    public static bool Runs(string groupId, string groupRoundId, Round round, IEnumerable<SittingOut> sittingOut) =>
        groupRoundId == round.Id || (groupRoundId.Length == 0 && SitsOut(groupId, round, sittingOut) is null);

    public static SittingOut? SitsOut(string groupId, Round round, IEnumerable<SittingOut> sittingOut) =>
        sittingOut.FirstOrDefault(each => each.GroupId == groupId && each.RoundId == round.Id);
}
