using System.Globalization;

namespace Gsbc.AppPoc.Data.Outreach;

public static class OutreachPosition
{
    public static GeoPoint? Parse(string? value)
    {
        var parts = value?.Split(',') ?? [];
        return parts.Length == 2
            && double.TryParse(parts[0], CultureInfo.InvariantCulture, out var lat)
            && double.TryParse(parts[1], CultureInfo.InvariantCulture, out var lon)
                ? new GeoPoint(lat, lon)
                : null;
    }
}

public static class OutreachFormat
{
    public static string Metres(double metres) => metres >= 1000 ? Kilometres(metres) : (Math.Round(metres / 10) * 10).ToString("0", CultureInfo.InvariantCulture) + "\u00a0m";

    public static string Kilometres(double metres) => (metres / 1000).ToString("0.0", CultureInfo.InvariantCulture) + "\u00a0km";

    public static string Duration(TimeSpan time)
    {
        var minutes = (int)Math.Round(time.TotalMinutes / 5) * 5;
        return minutes switch
        {
            < 60 => $"{Math.Max(minutes, 5)} min",
            _ when minutes % 60 == 0 => $"{minutes / 60} hr",
            _ => $"{minutes / 60} hr {minutes % 60} min",
        };
    }

    public static string Bundles(WalkPlan plan) => plan.BundlesToTake > plan.Bundles
        ? $"Take {plan.BundlesToTake} bundles: {plan.Bundles} plus a spare"
        : $"Take {plan.Bundles} {(plan.Bundles == 1 ? "bundle" : "bundles")}";

    public static string Claimers(IReadOnlyList<string> names) => names.Count switch
    {
        0 => "Nobody yet",
        1 => names[0],
        _ => $"{names[0]} + {names.Count - 1}",
    };

    public static string Window(OutreachDay day) =>
        $"{ChurchClock.Time(day.Start)}–{ChurchClock.Time(day.End)} from {day.Where}";

    public static string StreetList(WalkPlan plan, int take = 4) => StreetList(plan.Streets, take);

    public static string StreetList(IReadOnlyList<string> streets, int take = 4) =>
        streets.Count <= take ? string.Join(", ", streets) : $"{string.Join(", ", streets.Take(take))} and {streets.Count - take} more";

    public static string Asked(WalkBudget budget) => budget.Minutes is { } minutes
        ? Duration(TimeSpan.FromMinutes(minutes))
        : $"{(budget.Kilometres ?? 0).ToString("0.#", CultureInfo.InvariantCulture)}\u00a0km";

    public static string Reached(WalkBudget budget, WalkPlan plan) => budget.Minutes is null ? Kilometres(plan.Metres) : Duration(plan.Time);

    public static string ReachTitle(WalkReach reach, WalkBudget budget, WalkPlan plan, bool nearYou) => reach.Status switch
    {
        _ when !reach.Capped => $"This walk comes to {Reached(budget, plan)}",
        _ when budget.Minutes is null => $"Streets near {(nearYou ? "you" : "here")} run out at {Reached(budget, plan)}",
        _ => $"Streets near {(nearYou ? "you" : "here")} run out after about {Reached(budget, plan)}",
    };

    public static string Reason(ReachReason reason) => reason.Kind switch
    {
        ReachReasonKind.Rested => $"Streets done this week are left out, shown faded on the map: {StreetList(reason.Names)}.",
        ReachReasonKind.PreviewEdge => "This preview stops at the edge of Albany Creek. The app will reach further.",
        ReachReasonKind.BoxedIn => $"{Boundary(reason.Names)}{NonHomes(reason.Count)}.",
        ReachReasonKind.FarStreets => $"The nearest streets not done this week are {Metres(reason.Metres)} away, so getting there and back adds to it.",
        ReachReasonKind.NextTooLong => $"The next streets don't fit, so it stops short. A longer walk would cover about {reason.Count} letterboxes.",
        _ => "Both sides of the nearest streets already make it this long.",
    };

    public static string HopTitle(HopChoice hop) =>
        $"Add a {Metres(hop.Hop.Metres)} walk to reach {(hop.Street.Length > 0 ? hop.Street : "more streets")}";

    public static string HopPayoff(HopChoice hop, WalkBudget budget) =>
        $"About {hop.Added} more letterboxes, {Reached(budget, hop.Plan)} in all";

    public static string HopNote(WalkHop hop) => $"{Metres(hop.Metres)}, no letterboxes";

    public static string StartTitle(StartChoice start) =>
        $"Start from the {(start.Street.Length > 0 ? start.Street : "next")} area, {Metres(start.Metres)} away";

    public static string StartPayoff(StartChoice start, WalkBudget budget) =>
        $"Room for the full {Asked(budget)}: about {start.Plan.Letterboxes} letterboxes";

    public static string Most(WalkBudget budget, WalkPlan plan) => $"Up to {Reached(budget, plan)} nearby";

    private static string Boundary(IReadOnlyList<string> mainRoads) => mainRoads.Count > 0
        ? $"Past these streets are main roads we don't letterbox ({string.Join(" and ", mainRoads)})"
        : $"Past these streets it's more than {Metres(WalkPlan.MaxConnectorLegMetres)} to the next homes";

    private static string NonHomes(int count) => count switch
    {
        0 => "",
        1 => ", and a business we skip",
        _ => $", and {count} shops and other non-homes we skip",
    };
}
