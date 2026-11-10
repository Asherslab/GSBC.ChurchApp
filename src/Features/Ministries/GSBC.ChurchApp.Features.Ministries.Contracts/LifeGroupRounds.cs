using System.Globalization;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public enum RoundStage { BeingPlanned, ShownInTheApp, NowOn, Finished }

public static class LifeGroupRounds
{
    public static bool Runs(string groupId, string groupRoundId, Round round, IEnumerable<SittingOut> sittingOut) =>
        groupRoundId == round.Id || (groupRoundId.Length == 0 && SitsOut(groupId, round, sittingOut) is null);

    public static SittingOut? SitsOut(string groupId, Round round, IEnumerable<SittingOut> sittingOut) =>
        sittingOut.FirstOrDefault(each => each.GroupId == groupId && each.RoundId == round.Id);

    public static RoundStage StageOf(Round round, DateOnly today) =>
        Day(round.Sundays.LastOrDefault()) < today ? RoundStage.Finished
        : Day(round.Sundays.FirstOrDefault()) <= today ? RoundStage.NowOn
        : round.ShowsFrom.Length == 0 || Day(round.ShowsFrom) <= today ? RoundStage.ShownInTheApp
        : RoundStage.BeingPlanned;

    public static DateOnly Day(string? text) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day) ? day : DateOnly.MinValue;
}
