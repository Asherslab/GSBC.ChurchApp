using System.Globalization;
using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed record LifeGroupSet(Round Round, IReadOnlyList<MinistrySummary> Groups);

public sealed record LifeGroupSets(LifeGroupSet? Now, LifeGroupSet? Next, IReadOnlyList<MinistrySummary> AllYear)
{
    public static LifeGroupSets Of(LifeGroupsPage page, DateOnly today)
    {
        List<(Round Round, DateOnly First, DateOnly Last)> rounds = page.Rounds
            .Select(round => (round, Day(round.Sundays.FirstOrDefault()), Day(round.Sundays.LastOrDefault())))
            .Where(each => each.Item2 != DateOnly.MinValue)
            .OrderBy(each => each.Item2)
            .ToList();
        LifeGroupSet Set(Round round) => new(round, page.Groups.Where(group => group.RoundId == round.Id).ToList());
        (Round Round, DateOnly First, DateOnly Last)? now = rounds.Cast<(Round, DateOnly, DateOnly)?>().FirstOrDefault(each => each!.Value.Item2 <= today && today <= each.Value.Item3);
        (Round Round, DateOnly First, DateOnly Last)? next = rounds.Cast<(Round, DateOnly, DateOnly)?>()
            .FirstOrDefault(each => each!.Value.Item2 > today && (each.Value.Item1.ShowsFrom.Length == 0 || Day(each.Value.Item1.ShowsFrom) <= today));
        return new LifeGroupSets(
            now is { } current ? Set(current.Round) : null,
            next is { } coming ? Set(coming.Round) : null,
            page.Groups.Where(group => group.RoundId.Length == 0).ToList());
    }

    private static DateOnly Day(string? text) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day) ? day : DateOnly.MinValue;
}
