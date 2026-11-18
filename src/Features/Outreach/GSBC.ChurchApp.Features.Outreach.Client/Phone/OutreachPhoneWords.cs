using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Client.Location;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public static class OutreachPhoneWords
{
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;

    public const string Note = "Letterboxing with the church, at your own pace";
    public const string AskTitle = "Got an hour? Walk from your door";
    public const string AskBody = "The app makes a loop of streets the church hasn't walked lately. Put your phone away and walk: it marks the streets as you pass.";

    public static string Time(DateTime utc) => ChurchTime.ToChurch(utc).ToString("h:mmtt", English).Replace("AM", "am").Replace("PM", "pm").Replace(":00", "");

    public static string Clock(DateTime utc) => ChurchTime.ToChurch(utc).ToString("h:mmtt", English).Replace("AM", "am").Replace("PM", "pm");

    public static string DayOn(OutreachDay day) => $"Outreach is on till {Time(day.EndsAt)}";

    public static string DayWhen(OutreachDay day) =>
        $"{ChurchTime.ToChurch(day.StartsAt).ToString("ddd d MMM", English)}, {Time(day.StartsAt)} to {Time(day.EndsAt)}";

    public static string RoutesClaimed(int claimed, int routes) => routes == 0 ? "Routes coming soon" : $"{claimed} of {OutreachWords.Plural(routes, "route", "routes")} claimed";

    public static string Distance(double metres) =>
        metres < 1000 ? $"{Math.Round(metres / 10) * 10:0} m" : (metres / 1000).ToString(metres < 10_000 ? "0.0" : "0", English) + " km";

    public static string Duration(TimeSpan time)
    {
        int minutes = (int)Math.Round(time.TotalMinutes);
        if (minutes < 60)
            return $"{Math.Max(1, minutes)} min";
        int hours = minutes / 60, rest = minutes % 60;
        return rest == 0 ? $"{hours} hr" : $"{hours} hr {rest} min";
    }

    public static string Letterboxes(int count) => OutreachWords.Plural(count, "letterbox", "letterboxes");

    public static string Doors(int count) => OutreachWords.Plural(count, "door", "doors");

    public static string Bundles(int count, int bundleSize) =>
        count == 0 ? "No bundles needed" : $"Take {OutreachWords.Plural(count, "bundle", "bundles")} of {bundleSize}";

    public static string Carrying(IReadOnlyList<Campaign> carried) =>
        carried.Count == 0 ? OutreachCampaigns.GeneralName : string.Join(" and ", carried.Select(campaign => campaign.Name));

    public static string Reach(ReachReason reason) => reason.Kind switch
    {
        ReachReasonKind.AllDoneNearby => reason.Names.Count > 0
            ? $"The streets around here were all walked recently, {Names(reason.Names)} among them."
            : "The streets around here were all walked recently.",
        ReachReasonKind.Rested => $"{Names(reason.Names)} {(reason.Count == 1 ? "was" : "were")} walked recently, so they're left for now.",
        ReachReasonKind.AreaEdge => "This is the edge of the area the app's map covers.",
        ReachReasonKind.BoxedIn => reason.Names.Count > 0
            ? $"{Names(reason.Names)} and places that aren't homes bound the streets near you."
            : "Main roads and places that aren't homes bound the streets near you.",
        ReachReasonKind.FarStreets => $"The nearest streets not walked lately are {Distance(reason.Metres)} away.",
        ReachReasonKind.FirstStreet => "One street, both sides, already makes it this long.",
        ReachReasonKind.NextTooLong => "The next streets don't fit in the time you have.",
        _ => "",
    };

    public static string Short(WalkBudget budget, WalkPlan plan) =>
        budget.ByTime
            ? $"Streets near you run out at {Duration(plan.Time)}. You asked for {Duration(TimeSpan.FromMinutes(budget.Minutes ?? 0))}."
            : $"Streets near you run out at {Distance(plan.Metres)}. You asked for {Distance(budget.Asked)}.";

    public static string Long(WalkBudget budget, WalkPlan plan) =>
        budget.ByTime
            ? $"This walk is {Duration(plan.Time)}, longer than the {Duration(TimeSpan.FromMinutes(budget.Minutes ?? 0))} you asked for."
            : $"This walk is {Distance(plan.Metres)}, longer than the {Distance(budget.Asked)} you asked for.";

    public static string Hop(HopChoice hop) =>
        $"Walk {Distance(hop.Hop.Metres)} further to reach {hop.Street}: about {Letterboxes(hop.Added)} more, {Distance(hop.Plan.Metres)} in all";

    public static string Elsewhere(StartChoice start, WalkBudget budget) =>
        $"Start from the {start.Street} area, {Distance(start.Metres)} away: room for {(budget.ByTime ? Duration(TimeSpan.FromMinutes(budget.Minutes ?? 0)) : Distance(budget.Asked))}";

    public static string AllDone(StartChoice start) => $"The nearest good start is the {start.Street} area, {Distance(start.Metres)} away.";

    public const string FindingYou = "Finding you";
    public const string FindingYouNote = "Walk to the start. The app follows you once it has your location.";
    public const string RouteDone = "That's the route";
    public const string LastStreet = "That's the last street. Finish when you're done.";
    public const string RouteDoneNote = "Every street on the plan is done. Finish when you're ready.";

    public static string Side(bool houses) => houses ? "Houses on your left" : "Nothing to deliver here";

    public static string Turn(SavedStep from, SavedStep to) => TurnOf(from, to) switch
    {
        TurnKind.OtherSide => $"Then back along the other side of {to.Title}",
        TurnKind.Back => $"Then turn back into {to.Title}",
        TurnKind.Left => $"Then left into {to.Title}",
        TurnKind.Right => $"Then right into {to.Title}",
        _ => $"Then straight on into {to.Title}",
    };

    public static string TurnIcon(SavedStep from, SavedStep to) => TurnOf(from, to) switch
    {
        TurnKind.OtherSide or TurnKind.Back => "rotate-ccw",
        TurnKind.Left => "arrow-left",
        TurnKind.Right => "arrow-right",
        _ => "arrow-up",
    };

    public static WalkTurn TurnFor(SavedStep from, SavedStep to) => TurnOf(from, to) switch
    {
        TurnKind.OtherSide or TurnKind.Back => WalkTurn.Back,
        TurnKind.Left => WalkTurn.Left,
        TurnKind.Right => WalkTurn.Right,
        _ => WalkTurn.Straight,
    };

    private enum TurnKind
    {
        Straight,
        Left,
        Right,
        Back,
        OtherSide,
    }

    private static TurnKind TurnOf(SavedStep from, SavedStep to)
    {
        if (to.Street == from.Street)
            return TurnKind.OtherSide;
        if (from.Line.Count < 2 || to.Line.Count < 2)
            return TurnKind.Straight;
        double turn = (Bearing(to.Line[0], to.Line[1]) - Bearing(from.Line[^2], from.Line[^1]) + 540) % 360 - 180;
        return turn switch
        {
            < -150 or > 150 => TurnKind.Back,
            < -30 => TurnKind.Left,
            > 30 => TurnKind.Right,
            _ => TurnKind.Straight,
        };
    }

    private static readonly string[] Compass = ["north", "east", "south", "west"];

    public static string SideOf(SavedStep step) => Facing(step) is { } facing ? $"{step.Title}, {Compass[facing]} side" : step.Title;

    public static IReadOnlyList<string> SidesOf(IReadOnlyList<SavedStep> steps)
    {
        string[] sides = [.. steps.Select(SideOf)];
        foreach (List<int> same in Enumerable.Range(0, steps.Count).GroupBy(i => sides[i]).Select(group => group.ToList()).Where(group => group.Count > 1))
        {
            bool runsNorthSouth = Facing(steps[same[0]]) is 1 or 3;
            (string first, string last) = runsNorthSouth ? ("north", "south") : ("west", "east");
            List<int> ordered = [.. same.OrderBy(i => runsNorthSouth ? -Middle(steps[i])[0] : Middle(steps[i])[1])];
            for (int place = 0; place < ordered.Count; place++)
                sides[ordered[place]] += $", {Stretch(place, ordered.Count, first, last)}";
        }
        return sides;
    }

    private static int? Facing(SavedStep step)
    {
        if (step.Line.Count < 2)
            return null;
        double bearing = Bearing(step.Line[0], step.Line[^1]);
        return (int)Math.Round((bearing - 90 + 360) % 360 / 90) % 4;
    }

    private static double[] Middle(SavedStep step) =>
        step.Line.Count == 0 ? [0, 0] : [step.Line.Average(point => point[0]), step.Line.Average(point => point[1])];

    private static string Stretch(int place, int count, string first, string last) =>
        place == 0 ? $"{first} end"
        : place == count - 1 ? $"{last} end"
        : count == 3 ? "middle"
        : $"stretch {place + 1} of {count} from the {first}";

    public static string Remaining(int letterboxesLeft) => letterboxesLeft <= 0 ? "done" : $"{OutreachWords.Count(letterboxesLeft)} to go";

    public static string Walked(DateTime utc) => ChurchTime.ToChurch(utc).ToString("ddd d MMM", English);

    private static string Names(IReadOnlyList<string> names) =>
        names.Count switch
        {
            0 => "",
            1 => names[0],
            _ => $"{names[0]} and {names[1]}",
        };

    private static double Bearing(double[] a, double[] b)
    {
        double y = (b[0] - a[0]);
        double x = (b[1] - a[1]) * Math.Cos(a[0] * Math.PI / 180);
        return (90 - Math.Atan2(y, x) * 180 / Math.PI + 360) % 360;
    }
}
