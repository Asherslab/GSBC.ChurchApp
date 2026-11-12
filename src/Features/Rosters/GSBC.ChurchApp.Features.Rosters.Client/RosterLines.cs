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

    public static string Days(RosterDate first, RosterDate last)
    {
        string from = ChurchTime.ToChurch(first.StartsAt).ToString("ddd d MMM", CultureInfo.InvariantCulture);
        string to = ChurchTime.ToChurch(last.StartsAt).ToString("ddd d MMM", CultureInfo.InvariantCulture);
        return from == to ? from : $"{from} to {to}";
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

    public static string TrainingWith(Assignment trainee, IEnumerable<Assignment> samePosition)
    {
        List<string> trained = samePosition.Where(each => each.Id != trainee.Id && each.Fills && !each.Training).Select(each => First(each.Name)).ToList();
        return trained.Count == 0 ? "Training, with someone trained" : $"Training, with {string.Join(" and ", trained)}";
    }

    public static int Filled(RosterDate date, Position position) => In(date, position).Count(assignment => assignment.Fills);

    public static int Gaps(RosterView view, RosterDate date) => view.Positions.Sum(position => Math.Max(0, position.Needed - Filled(date, position)));

    public static string Summary(RosterView view, RosterDate date) =>
        string.Join(" · ", view.Positions.Select(position =>
        {
            List<string> names = In(date, position).Where(assignment => Replies.Fills(assignment.State))
                .Select(assignment => assignment.Training ? $"{First(assignment.Name)} (training)" : First(assignment.Name)).ToList();
            return $"{position.Name}: {(names.Count == 0 ? "nobody yet" : string.Join(", ", names))}";
        }));

    public static IReadOnlyList<string> FlagsIfOn(RosterView view, TeamMember member, RosterDate date, Position position)
    {
        List<string> flags = [];
        if (AwayRules.FlagFor(member.Away, ChurchTime.ToChurch(date.StartsAt), ChurchTime.ToChurch(date.EndsAt), date.AllDay) is { } away)
            flags.Add(away);
        if (position.NeedsBlueCard && !member.HoldsBlueCard)
            flags.Add("No current blue card");
        if (position.NeedsTraining && !member.TrainingCurrent)
            flags.Add("Safe Ministry training not current");
        if (Training.IsTrainee(position.NeedsTrained, position.Trained.Contains(member.PersonId)))
            flags.Add(In(date, position).Any(assignment => assignment.Fills) ? "Goes on training, with someone trained" : "Goes on training: needs someone trained on with them");
        flags.AddRange(date.Assignments.Where(assignment => assignment.PersonId == member.PersonId && Replies.Fills(assignment.State))
            .Select(assignment => $"Already on {assignment.PositionName}"));
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
        SwapWays.Direct => "They can ask someone on the team themselves",
        _ => way,
    };

    public static string AskerWords(string way, RosterSettings settings) => way switch
    {
        SwapWays.Direct => "You choose who on the team to ask, and they say yes or can't",
        SwapWays.Open when settings.OpenSwaps == OpenSwaps.Approve => "Anyone on the team can offer to take it, and a leader says yes",
        _ => "Anyone on the team can take it",
    };

    public static string OpenSwapsWords(string openSwaps) => openSwaps switch
    {
        OpenSwaps.Off => "Nobody takes it",
        OpenSwaps.Approve => "Anyone can offer, and a leader says yes",
        _ => "Anyone on the team can take it",
    };

    public static string OpenSwapsDetail(string openSwaps) => openSwaps switch
    {
        OpenSwaps.Off => "They ask a leader to find someone instead",
        OpenSwaps.Approve => "A leader chooses who takes it",
        _ => "Whoever takes it is on straight away",
    };

    public static string SwapSentence(RosterSettings settings) => (settings.OpenSwaps != OpenSwaps.Off, settings.DirectSwaps) switch
    {
        (false, false) => "So when someone can't do a date, they tell a leader, who finds someone else.",
        (true, false) => $"So when someone can't do a date, they ask the team{(settings.OpenSwaps == OpenSwaps.Approve ? ", and a leader says yes to an offer" : "")}. They stay on until it's taken.",
        (false, true) => "So when someone can't do a date, they ask someone on the team they choose. They stay on until it's taken.",
        (true, true) => $"So when someone can't do a date, they ask the whole team or someone they choose{(settings.OpenSwaps == OpenSwaps.Approve ? "; a leader says yes to an offer from the team" : "")}. They stay on until it's taken.",
    };

    public static IReadOnlyList<string> WaysOpenTo(RosterSettings settings) =>
        [.. settings.OpenSwaps != OpenSwaps.Off ? [SwapWays.Open] : Array.Empty<string>(), .. settings.DirectSwaps ? [SwapWays.Direct] : Array.Empty<string>()];

    public static string WhoSeesWords(string whoSees) => whoSees switch
    {
        WhoSees.WithYou => "Their own dates, and who's on with them",
        WhoSees.Yours => "Only their own dates",
        _ => "The whole roster",
    };

    public static string First(string name) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? name;

    public static string Wire(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string Span(AwaySpan span)
    {
        string times = TimesWords(span);
        if (span.Weekly)
        {
            string days = WeekdaysWords(span.Weekdays);
            return times.Length == 0 ? $"Every {days}" : $"{Capital(days)} {times}";
        }
        if (AwayRules.Day(span.From) is not { } from)
            return "";
        DateOnly to = AwayRules.Day(span.To) ?? from;
        string run = from == to ? from.ToString("dddd d MMMM", CultureInfo.InvariantCulture)
            : $"{from.ToString("d MMM", CultureInfo.InvariantCulture)} to {to.ToString("d MMM", CultureInfo.InvariantCulture)}";
        return times.Length == 0 ? run : $"{run}, {times}";
    }

    public static string SpanDetail(AwaySpan span, DateOnly today)
    {
        if (!span.Weekly)
            return "";
        List<string> parts = [];
        if (AwayRules.Day(span.From) is { } from && from > today)
            parts.Add($"From {from.ToString("d MMM", CultureInfo.InvariantCulture)}");
        parts.Add(AwayRules.Day(span.To) is { } to ? $"until {to.ToString("d MMM", CultureInfo.InvariantCulture)}" : "until further notice");
        List<DateOnly> except = span.Except.Select(AwayRules.Day).OfType<DateOnly>().Where(day => day >= today).Order().ToList();
        if (except.Count > 0)
            parts.Add($"not {string.Join(", ", except.Select(day => day.ToString("d MMM", CultureInfo.InvariantCulture)))}");
        return Capital(string.Join(", ", parts));
    }

    public static string TimesWords(AwaySpan span) => (AwayRules.Time(span.StartTime), AwayRules.Time(span.EndTime)) switch
    {
        (null, null) => "",
        (null, { } until) when until == MorningsEnd => "mornings",
        ({ } from, null) when from == EveningsStart => "evenings",
        (null, { } until) => $"until {ChurchDates.Time(DateOnly.MinValue.ToDateTime(until))}",
        ({ } from, null) => $"from {ChurchDates.Time(DateOnly.MinValue.ToDateTime(from))}",
        ({ } from, { } until) => $"{ChurchDates.Time(DateOnly.MinValue.ToDateTime(from))} to {ChurchDates.Time(DateOnly.MinValue.ToDateTime(until))}",
    };

    public static DayMark MarkOn(IEnumerable<AwaySpan> spans, DateOnly day)
    {
        List<AwaySpan> all = spans.ToList();
        List<DayMark> on = all.Where(span => AwayRules.OnDay(span, day)).Select(MarkOf).ToList();
        if (on.Contains(DayMark.AllDay))
            return DayMark.AllDay;
        if (on.Count > 0)
            return on.Distinct().Count() == 1 ? on[0] : DayMark.Part;
        return all.Any(span => AwayRules.Skips(span, day)) ? DayMark.Skipped : DayMark.None;
    }

    public static DayMark MarkOf(AwaySpan span) => (AwayRules.Time(span.StartTime), AwayRules.Time(span.EndTime)) switch
    {
        (null, null) => DayMark.AllDay,
        (null, { } until) when until <= MorningsEnd => DayMark.Morning,
        ({ } from, null) when from >= MorningsEnd => DayMark.Evening,
        _ => DayMark.Part,
    };

    public static string MarkWords(DayMark mark) => mark switch
    {
        DayMark.AllDay => "away all day",
        DayMark.Morning => "away in the morning",
        DayMark.Evening => "away in the evening",
        DayMark.Part => "away part of the day",
        DayMark.Skipped => "not away this time",
        _ => "",
    };

    public static readonly TimeOnly MorningsEnd = new(12, 0);
    public static readonly TimeOnly EveningsStart = new(17, 0);

    private static string WeekdaysWords(IReadOnlyCollection<int> weekdays) =>
        weekdays.Count == 7 ? "day"
        : string.Join(" and ", ((DayOfWeek[])[DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday])
            .Where(day => weekdays.Contains((int)day)).Select(day => day.ToString()));

    private static string Capital(string words) => words.Length == 0 ? words : char.ToUpperInvariant(words[0]) + words[1..];
}
