using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

public static class AnalyticsMeasures
{
    public static readonly Measure ActiveDay = new("app.active.day", "Phones today",
        "Phones and tablets that opened the app at least once that day.", OncePerPhone: true, SplitByGroup: true);

    public static readonly Measure ActiveWeek = new("app.active.week", "Phones this week",
        "Phones and tablets that opened the app at least once, Monday to Sunday. One person with two devices counts twice.", OncePerPhone: true, SplitByGroup: true);

    public static readonly Measure ActiveMonth = new("app.active.month", "Phones this month",
        "Phones and tablets that opened the app at least once this calendar month.", OncePerPhone: true, SplitByGroup: true);

    public static readonly Measure Installed = new("app.installed", "New phones",
        "Phones and tablets that opened the app for the first time. Reinstalling the app counts as new.", OncePerPhone: true);

    public static readonly Measure Returning = new("app.returning", "Came back",
        "Phones and tablets that opened the app this week and the week before.", OncePerPhone: true, SplitByGroup: true);

    public static readonly IReadOnlyList<Measure> All = [ActiveDay, ActiveWeek, ActiveMonth, Installed, Returning];
}
