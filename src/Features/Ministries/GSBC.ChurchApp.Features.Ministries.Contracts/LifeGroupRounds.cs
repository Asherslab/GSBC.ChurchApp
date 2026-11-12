using System.Globalization;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public enum RoundStage { BeingPlanned, ShownInTheApp, NowOn, Finished }

public static class LifeGroupRounds
{
    public static bool Runs(string groupId, string groupRoundId, Round round, IEnumerable<SittingOut> sittingOut) =>
        groupRoundId == round.Id || (groupRoundId.Length == 0 && SitsOut(groupId, round, sittingOut) is null);

    public static List<string> SundaysOf(string groupId, string groupRoundId, IEnumerable<Round> rounds, IEnumerable<SittingOut> sittingOut)
    {
        List<SittingOut> off = sittingOut.ToList();
        return rounds.Where(round => Runs(groupId, groupRoundId, round, off)).SelectMany(round => round.Sundays).Distinct().Order(StringComparer.Ordinal).ToList();
    }

    public static List<DaySpan> SpansOf(string groupId, string groupRoundId, IEnumerable<Round> rounds, IEnumerable<SittingOut> sittingOut)
    {
        List<SittingOut> off = sittingOut.ToList();
        return rounds.Where(round => round.Sundays.Count > 0 && Runs(groupId, groupRoundId, round, off))
            .Select(round => new DaySpan { From = round.Sundays.Min(StringComparer.Ordinal)!, To = round.Sundays.Max(StringComparer.Ordinal)! })
            .OrderBy(span => span.From, StringComparer.Ordinal)
            .ToList();
    }

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
