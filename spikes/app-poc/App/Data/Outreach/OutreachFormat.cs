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
    public static string Kilometres(double metres) => (metres / 1000).ToString("0.0", CultureInfo.InvariantCulture) + " km";

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

    public static string StreetList(WalkPlan plan, int take = 4) =>
        plan.Streets.Count <= take ? string.Join(", ", plan.Streets) : $"{string.Join(", ", plan.Streets.Take(take))} and {plan.Streets.Count - take} more";
}
