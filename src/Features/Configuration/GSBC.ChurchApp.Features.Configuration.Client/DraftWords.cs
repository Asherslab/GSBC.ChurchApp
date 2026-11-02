using System.Globalization;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Client;

public static class DraftWords
{
    public static string When(DateTime utc)
    {
        DateTime local = ChurchTime.ToChurch(utc);
        string format = local.Year == ChurchTime.ToChurch(DateTime.UtcNow).Year ? "ddd d MMM, h:mmtt" : "ddd d MMM yyyy, h:mmtt";
        return local.ToString(format, CultureInfo.InvariantCulture).Replace(":00", "").Replace("AM", "am").Replace("PM", "pm");
    }

    public static string Day(DateTime utc) => ChurchTime.ToChurch(utc).ToString("ddd d MMM", CultureInfo.InvariantCulture);

    public static string Touches(Draft draft, SurfaceCatalog surfaces)
    {
        List<string> areas = draft.Changes
            .Select(change => surfaces.Areas.GetValueOrDefault(change.Area)?.Title ?? change.Area)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();
        string count = draft.Changes.Count == 1 ? "1 change" : $"{draft.Changes.Count} changes";
        return areas.Count == 0 ? "No changes yet" : $"{string.Join(", ", areas)} · {count}";
    }

    public static string NameOf(ArrangedSurface entry) =>
        entry.Surface.Repeatable && entry.Surface.Settings.OfType<TextSetting>().FirstOrDefault() is { } title
            ? entry.Values.Get(title)
            : entry.Surface.Title;

    public static string Summary(ArrangedSurface entry)
    {
        if (entry.Surface == AppearanceSurfaces.CustomTheme.Surface)
            return "A theme to choose under Theme";
        List<string> parts = [entry.Hidden ? "hidden" : entry.Audience == Audience.Everyone ? "shown to everyone" : "shown to some people"];
        if (entry.StartsAt is { } starts)
            parts.Add($"from {Day(starts)}");
        if (entry.EndsAt is { } ends)
            parts.Add($"until {Day(ends.AddTicks(-1))}");
        if (entry.Surface.NeedsNewerApp)
            parts.Add($"needs app version {entry.Surface.IntroducedIn}");
        return string.Join(" · ", parts);
    }

    public static string Field(string field, Surface surface) => field switch
    {
        PatchField.Order => "its place in the order",
        PatchField.Hidden => "whether it's shown",
        PatchField.Audience => "who sees it",
        PatchField.Dates => "its dates",
        _ when PatchField.IsValue(field, out string key) => surface.Settings.FirstOrDefault(setting => setting.Key == key)?.Label.ToLowerInvariant() ?? key,
        _ => field,
    };
}
