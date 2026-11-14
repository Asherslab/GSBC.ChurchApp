using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed record Walker(Guid UserId, string Name, bool ByName);

internal static class TotalsRules
{
    public const int LeaderboardLength = 20;

    public static List<TotalsRow> Rows(IReadOnlyList<WalkRecord> walks, TotalsBy by) =>
        walks.GroupBy(walk => StartOf(walk, by))
            .Select(group => Totalled(new TotalsRow
            {
                Label = group.Key.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-AU")),
                Starts = ChurchTime.ToUtc(group.Key.ToDateTime(TimeOnly.MinValue)),
            }, group.ToList()))
            .OrderBy(row => row.Starts)
            .ToList();

    public static List<TotalsRow> ByCampaign(IReadOnlyList<WalkRecord> walks, IReadOnlyDictionary<Guid, string> campaignNames) =>
        walks.SelectMany(walk => walk.Campaigns.Select(campaign => (campaign, walk)))
            .GroupBy(pair => pair.campaign)
            .Select(group => Totalled(new TotalsRow
            {
                Label = campaignNames.GetValueOrDefault(group.Key, "A campaign since deleted"),
                Starts = group.Min(pair => pair.walk.StartedAt),
            }, group.Select(pair => pair.walk).ToList()))
            .OrderByDescending(row => row.Letterboxes)
            .ToList();

    public static (List<WalkerTotal> Named, WalkerTotal Anonymous) Walkers(IReadOnlyList<WalkRecord> walks, IReadOnlyDictionary<Guid, Walker> walkers)
    {
        List<WalkerTotal> named = walks.Where(walk => walk.UserId is { } user && walkers.TryGetValue(user, out Walker? walker) && walker.ByName)
            .GroupBy(walk => walk.UserId!.Value)
            .Select(group => Total(PersonNames.Short(walkers[group.Key].Name), group.ToList()))
            .OrderByDescending(total => total.Letterboxes).ThenByDescending(total => total.Metres).ThenBy(total => total.Name, StringComparer.Ordinal)
            .ToList();
        WalkerTotal anonymous = Total("", walks.Where(walk => walk.UserId is not { } user || !walkers.TryGetValue(user, out Walker? walker) || !walker.ByName).ToList());
        return (named, anonymous);
    }

    public static Leaderboard Leaderboard(Guid campaign, IReadOnlyList<WalkRecord> walks, IReadOnlyDictionary<Guid, Walker> walkers)
    {
        List<WalkRecord> theirs = walks.Where(walk => walk.Campaigns.Contains(campaign) && walk.Doubtful.Length == 0).ToList();
        (List<WalkerTotal> named, _) = Walkers(theirs, walkers);
        return new Leaderboard { Campaign = campaign.ToString(), Walkers = named.Take(LeaderboardLength).ToList(), Everyone = Total("", theirs) };
    }

    public static bool CountsByName(Guid? user, IReadOnlyDictionary<Guid, Walker> walkers) =>
        user is { } id && walkers.TryGetValue(id, out Walker? walker) && walker.ByName;

    private static DateOnly StartOf(WalkRecord walk, TotalsBy by)
    {
        DateOnly day = ChurchTime.DayOf(walk.StartedAt);
        return by switch
        {
            TotalsBy.Month => new DateOnly(day.Year, day.Month, 1),
            TotalsBy.Week => ChurchTime.WeekStart(day),
            _ => day,
        };
    }

    private static TotalsRow Totalled(TotalsRow row, List<WalkRecord> walks)
    {
        row.Walks = walks.Count;
        row.Metres = walks.Sum(walk => walk.Metres);
        row.Letterboxes = walks.Sum(walk => walk.Letterboxes);
        row.Doors = walks.Sum(walk => walk.Doors);
        row.Conversations = walks.Sum(walk => walk.Conversations);
        row.Walkers = walks.Select(walk => walk.UserId?.ToString() ?? walk.InstallId?.ToString() ?? walk.Id.ToString()).Distinct().Count();
        return row;
    }

    private static WalkerTotal Total(string name, List<WalkRecord> walks) => new()
    {
        Name = name, Walks = walks.Count, Metres = walks.Sum(walk => walk.Metres), Letterboxes = walks.Sum(walk => walk.Letterboxes),
        Doors = walks.Sum(walk => walk.Doors),
    };
}
