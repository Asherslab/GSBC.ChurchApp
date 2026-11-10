using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Calendars;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal static class LifeGroupSundays
{
    public const string Keeper = "ministries.life-groups";
    public const string Between = "Between groups";
    public const string TakenOff = "No Life Groups this Sunday";
    private const string Title = "Life Groups";
    private const string About = "Choose a group, and see where each one meets, in Life Groups.";
    private const int DaysInAWeek = 7;

    public static KeptRegularEvent Plan(IEnumerable<RoundRecord> rounds)
    {
        List<RoundRecord> planned = rounds.Where(round => round.Sundays.Count > 0).OrderBy(round => round.Sundays.Min()).ToList();
        List<KeptDate> on = planned.SelectMany(round => round.Sundays.Select(sunday => new KeptDate(sunday, round.Starts, round.Ends)))
            .GroupBy(date => date.Date).Select(group => group.First()).OrderBy(date => date.Date).ToList();
        List<KeptGap> notOn = [];
        if (on.Count > 0)
        {
            HashSet<DateOnly> meets = on.Select(date => date.Date).ToHashSet();
            for (DateOnly sunday = on[0].Date; sunday <= on[^1].Date; sunday = sunday.AddDays(DaysInAWeek))
                if (!meets.Contains(sunday))
                    notOn.Add(new KeptGap(sunday, planned.Any(round => round.Sundays.Min() < sunday && sunday < round.Sundays.Max()) ? TakenOff : Between));
        }
        return new KeptRegularEvent(Keeper, Title, MinistriesDestinations.LifeGroups.Path, Title, About, on, notOn);
    }
}
