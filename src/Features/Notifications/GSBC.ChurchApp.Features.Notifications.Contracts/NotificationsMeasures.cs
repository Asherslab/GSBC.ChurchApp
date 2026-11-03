using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Features.Notifications.Contracts;

public static class NotificationsMeasures
{
    public const string On = "on";
    public const string Off = "off";
    public const string Undecided = "undecided";

    public static readonly Measure Push = new("notifications.push", "Notifications",
        "Phones that used the app this week, by whether notifications are on for it. Each phone counts once a week, the first time it's closed.",
        OncePerPhone: true,
        States: [new(On, "On"), new(Off, "Turned off"), new(Undecided, "Not chosen yet")]);

    public static readonly IReadOnlyList<Measure> All = [Push];
}
