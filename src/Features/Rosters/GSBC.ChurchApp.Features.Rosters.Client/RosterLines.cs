using System.Globalization;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Rosters.Client;

public sealed record YourDate(RosterView View, RosterDate Date, Assignment Assignment, PersonLine For)
{
    public bool Yourself => For.Id == View.YourId;
}

public static class RosterLines
{
    public const string AwayFlag = "Away that day";

    public static IReadOnlyList<YourDate> Yours(RosterView view, DateTime nowUtc)
    {
        Dictionary<string, PersonLine> people = view.You.ToDictionary(person => person.Id);
        return view.Dates
            .Where(date => date.EndsAt > nowUtc)
            .SelectMany(date => date.Assignments
                .Where(assignment => people.ContainsKey(assignment.PersonId) && Replies.Fills(assignment.State))
                .Select(assignment => new YourDate(view, date, assignment, people[assignment.PersonId])))
            .OrderBy(each => each.Date.StartsAt)
            .ToList();
    }

    public static IReadOnlyList<YourDate> Yours(IEnumerable<RosterView> views, DateTime nowUtc) =>
        views.SelectMany(view => Yours(view, nowUtc)).OrderBy(each => each.Date.StartsAt).ToList();

    public static IReadOnlyList<(RosterDate Date, Assignment Assignment)> OpenToYou(RosterView view, DateTime nowUtc)
    {
        HashSet<string> yours = view.You.Select(person => person.Id).ToHashSet();
        return view.Dates
            .Where(date => date.StartsAt > nowUtc)
            .SelectMany(date => date.Assignments.Select(assignment => (date, assignment)))
            .Where(each => !yours.Contains(each.assignment.PersonId) && each.assignment.Swap is { State: SwapStates.Open } swap
                           && (swap.Way == SwapWays.Direct ? yours.Contains(swap.AskedToId) : yours.Count > 0))
            .ToList();
    }

    public static string When(RosterDate date)
    {
        DateTime local = ChurchTime.ToChurch(date.StartsAt);
        string day = local.ToString("ddd d MMM", CultureInfo.InvariantCulture);
        return date.AllDay ? day : $"{day} · {ChurchDates.Time(local)}";
    }

    public static string LongWhen(RosterDate date)
    {
        DateTime local = ChurchTime.ToChurch(date.StartsAt);
        string day = local.ToString("dddd d MMMM", CultureInfo.InvariantCulture);
        return date.AllDay ? day : $"{day}, {ChurchDates.Time(local)} to {ChurchDates.Time(ChurchTime.ToChurch(date.EndsAt))}";
    }

    public static string DayNumber(RosterDate date) => ChurchTime.ToChurch(date.StartsAt).Day.ToString(CultureInfo.InvariantCulture);

    public static string Weekday(RosterDate date, DateTime nowUtc)
    {
        DateTime local = ChurchTime.ToChurch(date.StartsAt);
        int days = local.Date.Subtract(ChurchTime.ToChurch(nowUtc).Date).Days;
        return days switch
        {
            0 => "today",
            1 => "tomorrow",
            < 7 => local.ToString("dddd", CultureInfo.InvariantCulture),
            _ => local.ToString("dddd d MMM", CultureInfo.InvariantCulture),
        };
    }

    public static string StateWords(Assignment assignment) => assignment.State switch
    {
        Replies.Asked => "No answer yet",
        Replies.Accepted => "Can do it",
        Replies.Declined => "Can't do it",
        _ => "",
    };

    public static PillTone ToneOf(Assignment assignment) => assignment.State switch
    {
        Replies.Accepted => PillTone.Accent,
        Replies.Declined => PillTone.Danger,
        _ => PillTone.Marker,
    };

    public static IReadOnlyList<Assignment> In(RosterDate date, Position position) =>
        date.Assignments.Where(assignment => assignment.PositionId == position.Id).ToList();

    public static int Filled(RosterDate date, Position position) => In(date, position).Count(assignment => Replies.Fills(assignment.State));

    public static int Gaps(RosterView view, RosterDate date) => view.Positions.Sum(position => Math.Max(0, position.Needed - Filled(date, position)));

    public static string Summary(RosterView view, RosterDate date) =>
        string.Join(" · ", view.Positions.Select(position =>
        {
            List<string> names = In(date, position).Where(assignment => Replies.Fills(assignment.State)).Select(assignment => First(assignment.Name)).ToList();
            return $"{position.Name}: {(names.Count == 0 ? "nobody yet" : string.Join(", ", names))}";
        }));

    public static IReadOnlyList<string> FlagsIfOn(RosterView view, TeamMember member, RosterDate date, Position position)
    {
        DateOnly day = ChurchTime.DayOf(date.StartsAt);
        List<string> flags = [];
        if (member.Away.Any(span => DateOnly.TryParse(span.From, CultureInfo.InvariantCulture, out DateOnly from)
                                    && DateOnly.TryParse(span.To, CultureInfo.InvariantCulture, out DateOnly to) && from <= day && day <= to))
            flags.Add(AwayFlag);
        if (position.NeedsBlueCard && !member.HoldsBlueCard)
            flags.Add("No current blue card");
        if (position.NeedsTraining && !member.TrainingCurrent)
            flags.Add("Safe Ministry training not current");
        if (position.NeedsTrained && !position.Trained.Contains(member.PersonId))
            flags.Add($"Not trained for {position.Name.ToLowerInvariant()}");
        flags.AddRange(date.Assignments.Where(assignment => assignment.PersonId == member.PersonId && Replies.Fills(assignment.State))
            .Select(assignment => $"Already on {assignment.PositionName.ToLowerInvariant()}"));
        return flags;
    }

    public static string Needs(Position position)
    {
        List<string> needs = [];
        if (position.NeedsBlueCard)
            needs.Add("a blue card");
        if (position.NeedsTraining)
            needs.Add("Safe Ministry training");
        if (position.NeedsTrained)
            needs.Add("being trained for it");
        string people = position.Needed == 1 ? "1 person" : $"{position.Needed} people";
        return needs.Count == 0 ? people : $"{people} · needs {string.Join(", ", needs)}";
    }

    public static string WaysWords(string way) => way switch
    {
        SwapWays.Team => "Anyone on the team can take it",
        SwapWays.Approve => "Anyone on the team can offer, and a leader says yes",
        SwapWays.Direct => "Ask someone on the team yourself",
        _ => way,
    };

    public static string WhoSeesWords(string whoSees) => whoSees switch
    {
        WhoSees.WithYou => "Their own dates, and who's on with them",
        WhoSees.Yours => "Only their own dates",
        _ => "The whole roster",
    };

    public static string First(string name) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? name;

    public static string Wire(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string Span(AwaySpan span) =>
        DateOnly.TryParse(span.From, CultureInfo.InvariantCulture, out DateOnly from) && DateOnly.TryParse(span.To, CultureInfo.InvariantCulture, out DateOnly to)
            ? from == to ? from.ToString("dddd d MMMM", CultureInfo.InvariantCulture)
            : $"{from.ToString("d MMM", CultureInfo.InvariantCulture)} to {to.ToString("d MMM", CultureInfo.InvariantCulture)}"
            : "";
}
