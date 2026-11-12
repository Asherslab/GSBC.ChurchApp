using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Api.People;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed record AwayDays(DateOnly From, DateOnly To)
{
    public bool Covers(DateOnly day) => From <= day && day <= To;
}

internal static class RosterRules
{
    public const int MostDaysBefore = 28;
    public const string AwayFlag = "Away that day";
    public const string NoBlueCardFlag = "No current blue card";
    public const string NoTrainingFlag = "Safe Ministry training not current";

    public static IReadOnlyList<string> Flags(PositionRecord position, DateOnly day, IEnumerable<AwayDays> away, SafeguardingStanding? standing, bool aloneTraining,
        IEnumerable<string> alsoOn)
    {
        List<string> flags = [];
        if (away.Any(span => span.Covers(day)))
            flags.Add(AwayFlag);
        if (position.NeedsBlueCard && standing?.HoldsBlueCard != true)
            flags.Add(NoBlueCardFlag);
        if (position.NeedsTraining && standing?.TrainingCurrent != true)
            flags.Add(NoTrainingFlag);
        if (aloneTraining)
            flags.Add(Training.AloneFlag);
        flags.AddRange(alsoOn.Select(other => $"Also on {other} that day"));
        return flags;
    }

    public static RosterDate? Visible(RosterDate date, string whoSees, bool leads, IReadOnlySet<string> yours)
    {
        if (leads || whoSees == WhoSees.Everything)
            return date;

        bool youreOn = date.Assignments.Any(assignment => yours.Contains(assignment.PersonId));
        if (whoSees == WhoSees.WithYou && youreOn)
            return date;

        List<Assignment> kept = date.Assignments.Where(assignment => yours.Contains(assignment.PersonId) || OpenTo(assignment.Swap, yours)).ToList();
        return kept.Count == 0 ? null : Copy(date, kept);
    }

    public static bool OpenTo(SwapLine? swap, IReadOnlySet<string> yours) => swap is { State: SwapStates.Open } open
        && (open.Way == SwapWays.Direct ? yours.Contains(open.AskedToId) : yours.Count > 0);

    public static string? WhyNotChange(bool archived, DateTime startsAt, DateTime now) =>
        archived ? "This ministry is archived, so its roster can't change."
        : startsAt <= now ? "This date has passed."
        : null;

    public static string? WhyNotReply(bool mayAct, bool archived, bool repliesOn, DateTime startsAt, DateTime now) =>
        !mayAct ? "Only they, or a parent of theirs, can answer for this date."
        : WhyNotChange(archived, startsAt, now) ?? (repliesOn ? null : "This roster doesn't ask for answers.");

    public static string? WayFor(RosterSettings settings, string asked) =>
        asked == SwapWays.Direct ? settings.DirectSwaps ? SwapWays.Direct : null
        : settings.OpenSwaps == OpenSwaps.Off ? null
        : settings.OpenSwaps;

    public static string? WhyNotAskSwap(bool mayAct, bool archived, string? way, string state, bool alreadyAsked, DateTime startsAt, DateTime now, bool toIsOnTeam) =>
        !mayAct ? "Only they, or a parent of theirs, can ask for a swap."
        : WhyNotChange(archived, startsAt, now)
          ?? (way is null ? "This roster doesn't swap that way."
              : !Replies.Fills(state) ? "They've said they can't do it, so a leader finds someone else."
              : alreadyAsked ? "A swap is already asked for this date."
              : way == SwapWays.Direct && !toIsOnTeam ? "Choose someone on the team to ask."
              : null);

    public static string? WhyNotTake(SwapRecord swap, Guid taker, Guid? onNow, bool takerOnTeam, bool mayAct, bool archived, DateTime startsAt, DateTime now) =>
        !mayAct ? "Only they, or a parent of theirs, can take it."
        : WhyNotChange(archived, startsAt, now)
          ?? (swap.State != SwapStates.Open ? "Someone has already taken it, or it's been called off."
              : swap.Way == SwapWays.Direct && swap.AskedTo != taker ? "This swap was asked of someone else."
              : onNow == taker ? "They're already on for it."
              : !takerOnTeam ? "Only the team can take a place on the roster."
              : null);

    public static string AfterTaking(string way) => way == SwapWays.Approve ? SwapStates.Offered : SwapStates.Done;

    public static bool ReminderDue(string state, DateTime startsAt, DateTime now, int daysBefore, DateTime? remindedAt) =>
        daysBefore > 0 && remindedAt is null && Replies.Fills(state) && startsAt > now && startsAt - now <= TimeSpan.FromDays(daysBefore);

    public static bool NudgeDue(string state, DateTime assignedAt, DateTime startsAt, DateTime now, int afterDays, DateTime? nudgedAt) =>
        afterDays > 0 && nudgedAt is null && state == Replies.Asked && startsAt > now && now - assignedAt >= TimeSpan.FromDays(afterDays);

    public static string FirstState(bool repliesOn) => repliesOn ? Replies.Asked : Replies.On;

    public static RosterSettings Normalised(RosterSettings settings) => new()
    {
        Replies = settings.Replies,
        OpenSwaps = OpenSwaps.Valid(settings.OpenSwaps) ? settings.OpenSwaps : OpenSwaps.Team,
        DirectSwaps = settings.DirectSwaps,
        Calendars = settings.Calendars.Select(id => Guid.TryParse(id, out Guid calendar) ? calendar : Guid.Empty).Where(id => id != Guid.Empty).Distinct()
            .Select(id => id.ToString()).ToList(),
        WhoSees = WhoSees.Valid(settings.WhoSees) ? settings.WhoSees : WhoSees.Everything,
        RemindDaysBefore = Math.Clamp(settings.RemindDaysBefore, 0, MostDaysBefore),
        NudgeAfterDays = Math.Clamp(settings.NudgeAfterDays, 0, MostDaysBefore),
    };

    public static RosterSettings SettingsOf(SettingsRecord? record) => record is null
        ? new RosterSettings()
        : new RosterSettings
        {
            Replies = record.Replies, OpenSwaps = OpenSwaps.Valid(record.OpenSwaps) ? record.OpenSwaps : OpenSwaps.Team, DirectSwaps = record.DirectSwaps,
            Calendars = record.Calendars.Select(id => id.ToString()).ToList(),
            WhoSees = WhoSees.Valid(record.WhoSees) ? record.WhoSees : WhoSees.Everything,
            RemindDaysBefore = record.RemindDaysBefore, NudgeAfterDays = record.NudgeAfterDays,
        };

    public static string? WhyNotAway(DateOnly from, DateOnly to, DateOnly today) =>
        to < from ? "The last day can't be before the first."
        : to < today ? "Those days have passed."
        : null;

    public static string ForWords(string actorName, string personName) => $"{First(actorName)}, for {First(personName)}";

    public static string First(string name) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? name;

    private static RosterDate Copy(RosterDate date, List<Assignment> assignments) => new()
    {
        Id = date.Id, EventKey = date.EventKey, Title = date.Title, CalendarName = date.CalendarName, StartsAt = date.StartsAt, EndsAt = date.EndsAt,
        AllDay = date.AllDay, NotOn = date.NotOn, Assignments = assignments,
    };
}
