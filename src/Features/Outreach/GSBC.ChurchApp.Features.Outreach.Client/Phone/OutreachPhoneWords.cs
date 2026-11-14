using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
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

    public static string Campaigns(IEnumerable<Campaign> carried) => string.Join(" and ", carried.Select(campaign => campaign.Name));

    public static string Carrying(IReadOnlyList<Campaign> carried) =>
        carried.Count == 0 ? "General outreach" : $"Carrying {string.Join(" and ", carried.Select(HandedOut))}";

    private static string HandedOut(Campaign campaign) => campaign.HandedOut.Trim() is { Length: > 0 } what ? what : campaign.Name;

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

    public static string Side(bool houses) => houses ? "Houses on your left" : "No letterboxes on this stretch";

    public static string Turn(SavedStep from, SavedStep to)
    {
        if (to.Street == from.Street)
            return $"Then back along the other side of {to.Title}";
        if (from.Line.Count < 2 || to.Line.Count < 2)
            return $"Then {to.Title}";
        double inBearing = Bearing(from.Line[^2], from.Line[^1]);
        double outBearing = Bearing(to.Line[0], to.Line[1]);
        double turn = (outBearing - inBearing + 540) % 360 - 180;
        string way = turn switch
        {
            < -150 or > 150 => "turn back into",
            < -30 => "left into",
            > 30 => "right into",
            _ => "straight on into",
        };
        return $"Then {way} {to.Title}";
    }

    public static string Remaining(int letterboxesLeft) => letterboxesLeft <= 0 ? "Done" : $"{Letterboxes(letterboxesLeft)} to go";

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
