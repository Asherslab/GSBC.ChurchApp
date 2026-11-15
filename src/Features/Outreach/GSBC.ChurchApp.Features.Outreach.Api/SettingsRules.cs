using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal static class SettingsRules
{
    public static string? WhyNot(OutreachSettings settings) =>
        Outside(settings.RestDays, 1, 730) ? "The rest period is between a day and two years."
        : Outside(settings.WalkingKmh, 2, 7) ? "The walking pace is between 2 and 7 km/h."
        : Outside(settings.HouseSeconds, 1, 120) || Outside(settings.UnitSeconds, 1, 120) ? "A letterbox takes between 1 and 120 seconds."
        : Outside(settings.DoorSeconds, 10, 900) || Outside(settings.UnitDoorSeconds, 10, 900) ? "A door takes between 10 seconds and 15 minutes."
        : Outside(settings.BundleSize, 1, 1000) ? "A bundle holds between 1 and 1,000."
        : Outside(settings.SpareShare, 0, 0.5) ? "A spare bundle is taken when up to half a bundle is left over."
        : Outside(settings.MaxConnectorLegMetres, 0, 2000) || Outside(settings.MaxConnectorTotalMetres, 0, 5000)
          || Outside(settings.SplitJoinMetres, 0, 2000) || Outside(settings.MaxHopMetres, 0, 5000) || Outside(settings.MaxApproachMetres, 0, 5000) ? "Walks between streets are up to 2 km (5 km in all)."
        : Outside(settings.ShortOf, 0.5, 1) || Outside(settings.LongOf, 1, 3) ? "A walk is said to fall short below half what was asked, and too long up to three times."
        : Outside(settings.NoJunkMailKeptDays, 30, 3650) ? "No junk mail marks are kept between a month and ten years."
        : Outside(settings.FollowUpsKeptDays, 7, 730) ? "Follow-ups are kept between a week and two years."
        : Outside(settings.FastestKmh, 5, 20) ? "The fastest walk counted is between 5 and 20 km/h."
        : Outside(settings.LongestWalkKm, 5, 60) ? "The longest walk counted is between 5 and 60 km."
        : null;

    private static bool Outside(double value, double least, double most) => double.IsNaN(value) || value < least || value > most;
}
